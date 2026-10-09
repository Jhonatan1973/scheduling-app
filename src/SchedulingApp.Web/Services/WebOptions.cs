namespace SchedulingApp.Web.Services;

public sealed class WebOptions
{
    public const string SectionName = "Web";

    /// <summary>Shows the seeded demo credentials on the login page (portfolio deploys).</summary>
    public bool ShowDemoAccounts { get; set; }
}
