namespace SchedulingApp.Application.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@scheduling-app.local";
    public string FromName { get; set; } = "Scheduling App";
}
