namespace SchedulingApp.Application.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Public URL of the Blazor front-end, used for links inside emails.</summary>
    public string PublicWebUrl { get; set; } = "http://localhost:5126";
}
