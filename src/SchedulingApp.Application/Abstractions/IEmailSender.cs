namespace SchedulingApp.Application.Abstractions;

public sealed record EmailMessage(string ToEmail, string ToName, string Subject, string HtmlBody, string TextBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
