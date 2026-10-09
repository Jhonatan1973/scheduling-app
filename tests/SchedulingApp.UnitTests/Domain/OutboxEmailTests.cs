using SchedulingApp.Domain.Entities;
using static SchedulingApp.UnitTests.Support.Builders;

namespace SchedulingApp.UnitTests.Domain;

public class OutboxEmailTests
{
    private static OutboxEmail Email() => new()
    {
        ToEmail = "a@test.com",
        ToName = "A",
        Subject = "S",
        HtmlBody = "<p>x</p>",
        TextBody = "x"
    };

    [Fact]
    public void Failed_attempts_use_exponential_backoff()
    {
        var now = Utc(2030, 1, 1, 12);
        var email = Email();

        email.MarkFailedAttempt("smtp down", now);
        Assert.Equal(now.AddSeconds(30), email.NextAttemptAtUtc);

        email.MarkFailedAttempt("smtp down", now);
        Assert.Equal(now.AddSeconds(60), email.NextAttemptAtUtc);
        Assert.Equal(OutboxEmailStatus.Pending, email.Status);
    }

    [Fact]
    public void Gives_up_after_max_attempts()
    {
        var email = Email();

        for (var i = 0; i < OutboxEmail.MaxAttempts; i++)
            email.MarkFailedAttempt("boom", Utc(2030, 1, 1));

        Assert.Equal(OutboxEmailStatus.Failed, email.Status);
        Assert.Equal(OutboxEmail.MaxAttempts, email.Attempts);
    }

    [Fact]
    public void Mark_sent_clears_the_last_error()
    {
        var email = Email();
        email.MarkFailedAttempt("boom", Utc(2030, 1, 1));

        email.MarkSent(Utc(2030, 1, 1, 0, 1));

        Assert.Equal(OutboxEmailStatus.Sent, email.Status);
        Assert.Null(email.LastError);
    }
}
