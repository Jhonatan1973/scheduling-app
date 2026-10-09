namespace SchedulingApp.IntegrationTests;

/// <summary>
/// Integration tests need a real PostgreSQL. They run when either:
///  - INTEGRATION_DB is set to a PostgreSQL connection string (no Docker needed), or
///  - Docker is available (Testcontainers starts a throw-away PostgreSQL).
/// Otherwise they are skipped instead of failing.
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!TestEnvironment.CanRun)
            Skip = "Set INTEGRATION_DB to a PostgreSQL connection string or start Docker to run integration tests.";
    }
}

public static class TestEnvironment
{
    public static string? ExternalConnectionString =>
        Environment.GetEnvironmentVariable("INTEGRATION_DB") is { Length: > 0 } cs ? cs : null;

    public static bool DockerAvailable
    {
        get
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST")))
                return true;

            if (!OperatingSystem.IsWindows())
                return File.Exists("/var/run/docker.sock");

            try
            {
                return Directory.GetFiles(@"\\.\pipe\")
                    .Any(p => p.EndsWith("docker_engine", StringComparison.OrdinalIgnoreCase)
                           || p.EndsWith("dockerDesktopLinuxEngine", StringComparison.OrdinalIgnoreCase));
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    public static bool CanRun => ExternalConnectionString is not null || DockerAvailable;
}
