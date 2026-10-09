using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SchedulingApp.Application.Contracts.Messaging;
using SchedulingApp.Application.Options;

namespace SchedulingApp.Infrastructure.Email;

public class EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAppointmentNotificationAsync(AppointmentNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var subject = message.EventType switch
        {
            "Booked" => "Appointment confirmed",
            "Cancelled" => "Appointment cancelled",
            _ => "Appointment update"
        };

        var body = $"""
            Hello {message.RecipientName},

            Your appointment with {message.ProfessionalName} was {message.EventType.ToLowerInvariant()}.

            When: {message.StartUtc:yyyy-MM-dd HH:mm} UTC
            Until: {message.EndUtc:yyyy-MM-dd HH:mm} UTC

            — Scheduling App
            """;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.RecipientEmail));
        mime.Subject = subject;
        mime.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            logger.LogInformation("Email sent to {Email} for appointment {AppointmentId}", message.RecipientEmail, message.AppointmentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", message.RecipientEmail);
            throw;
        }
    }
}
