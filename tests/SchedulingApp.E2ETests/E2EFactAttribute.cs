namespace SchedulingApp.E2ETests;

/// <summary>
/// End-to-end tests run against a running deployment (local or CI).
/// They are skipped unless E2E_BASE_URL is set, so a plain `dotnet test` stays green offline.
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL")))
            Skip = "Set E2E_BASE_URL (e.g. http://localhost:5126) to run end-to-end tests.";
    }
}
