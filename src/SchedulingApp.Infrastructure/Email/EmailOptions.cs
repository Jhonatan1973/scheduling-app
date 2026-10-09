namespace SchedulingApp.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Log" (default, prints emails to the console), "Smtp" (e.g. Mailpit locally) or "Brevo" (HTTP API, works on hosts that block SMTP).</summary>
    public string Provider { get; set; } = "Log";

    public string FromEmail { get; set; } = "no-reply@scheduling-app.local";
    public string FromName { get; set; } = "Scheduling App";

    public SmtpSettings Smtp { get; set; } = new();
    public BrevoSettings Brevo { get; set; } = new();
    public DispatcherSettings Dispatcher { get; set; } = new();

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;
        public bool UseStartTls { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public sealed class BrevoSettings
    {
        public string? ApiKey { get; set; }
        public string BaseUrl { get; set; } = "https://api.brevo.com/";
    }

    public sealed class DispatcherSettings
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 20;
    }
}
