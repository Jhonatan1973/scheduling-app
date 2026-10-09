using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure.Email;

/// <summary>
/// Background worker that delivers emails stored in the outbox table, with exponential backoff retries.
/// Because rows are written in the same transaction as the appointment change, no email is lost
/// and no email is sent for a change that was rolled back.
/// </summary>
public sealed partial class OutboxEmailDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<OutboxEmailDispatcher> logger) : BackgroundService
{
    private readonly EmailOptions.DispatcherSettings _settings = options.Value.Dispatcher;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _settings.IntervalSeconds)));
        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogDispatchFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Processes one batch. Public so integration tests can trigger delivery deterministically.</summary>
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = clock.GetUtcNow().UtcDateTime;

        var batch = await db.OutboxEmails
            .Where(e => e.Status == OutboxEmailStatus.Pending && e.NextAttemptAtUtc <= now)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var email in batch)
        {
            try
            {
                await sender.SendAsync(
                    new EmailMessage(email.ToEmail, email.ToName, email.Subject, email.HtmlBody, email.TextBody),
                    cancellationToken);
                email.MarkSent(clock.GetUtcNow().UtcDateTime);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                email.MarkFailedAttempt(ex.Message, clock.GetUtcNow().UtcDateTime);
                LogSendFailed(ex, email.Id, email.Attempts);
            }
        }

        if (batch.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return batch.Count;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox dispatch cycle failed")]
    private partial void LogDispatchFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send outbox email {EmailId} (attempt {Attempt})")]
    private partial void LogSendFailed(Exception ex, Guid emailId, int attempt);
}
