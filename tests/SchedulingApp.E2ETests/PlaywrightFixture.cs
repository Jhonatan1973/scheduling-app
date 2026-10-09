using Microsoft.Playwright;

namespace SchedulingApp.E2ETests;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public static string BaseUrl => (Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5126").TrimEnd('/');

    private IPlaywright? _playwright;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL")))
            return; // tests are skipped

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            SlowMo = Environment.GetEnvironmentVariable("HEADED") == "1" ? 250 : 0
        });
    }

    /// <summary>New isolated browser context with tracing (saved under playwright-traces/ for CI artifacts).</summary>
    public async Task<(IBrowserContext Context, IPage Page)> NewPageAsync(string testName, ViewportSize? viewport = null)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            ViewportSize = viewport ?? new ViewportSize { Width = 1280, Height = 860 },
            Locale = "en-US"
        });
        context.SetDefaultTimeout(30_000);
        await context.Tracing.StartAsync(new TracingStartOptions { Screenshots = true, Snapshots = true, Title = testName });
        return (context, await context.NewPageAsync());
    }

    public static async Task CloseAsync(IBrowserContext context, string testName)
    {
        Directory.CreateDirectory("playwright-traces");
        await context.Tracing.StopAsync(new TracingStopOptions { Path = Path.Combine("playwright-traces", $"{testName}.zip") });
        await context.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_playwright is null)
            return;

        await Browser.CloseAsync();
        _playwright.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "e2e";
}
