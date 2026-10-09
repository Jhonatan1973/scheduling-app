namespace SchedulingApp.Domain.Entities;

public enum OutboxEmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

/// <summary>
/// Transactional outbox row: written in the same transaction as the business change,
/// delivered later by a background dispatcher with retries.
/// </summary>
public class OutboxEmail
{
    public const int MaxAttempts = 5;

    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ToEmail { get; set; }
    public required string ToName { get; set; }
    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public required string TextBody { get; set; }
    public OutboxEmailStatus Status { get; set; } = OutboxEmailStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }

    public void MarkSent(DateTime nowUtc)
    {
        Status = OutboxEmailStatus.Sent;
        SentAtUtc = nowUtc;
        LastError = null;
    }

    public void MarkFailedAttempt(string error, DateTime nowUtc)
    {
        Attempts++;
        LastError = error.Length > 1000 ? error[..1000] : error;

        if (Attempts >= MaxAttempts)
        {
            Status = OutboxEmailStatus.Failed;
            return;
        }

        // Exponential backoff: 30s, 1m, 2m, 4m...
        NextAttemptAtUtc = nowUtc.AddSeconds(30 * Math.Pow(2, Attempts - 1));
    }
}
