using Microsoft.Extensions.Logging;
using SchedulingApp.Application.Abstractions;

namespace SchedulingApp.Infrastructure.Email;

/// <summary>Development/demo sender: writes the email to the log instead of delivering it.</summary>
public sealed partial class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogEmail(message.ToEmail, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[email] To: {To} | Subject: {Subject}\n{Body}")]
    private partial void LogEmail(string to, string subject, string body);
}
