namespace SchedulingApp.Infrastructure.Persistence;

/// <summary>
/// Accepts both Npgsql keyword strings and URL strings (postgres://user:pass@host:5432/db?sslmode=require)
/// as provided by Render, Neon, Railway, Heroku...
/// </summary>
public static class ConnectionStringResolver
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        var parts = new List<string>
        {
            $"Host={uri.Host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={user}",
            $"Password={password}"
        };

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var sslMode = query["sslmode"];
        var mapped = sslMode?.ToLowerInvariant() switch
        {
            "disable" => "Disable",
            "allow" => "Allow",
            "prefer" => "Prefer",
            "require" => "Require",
            "verify-ca" => "VerifyCA",
            "verify-full" => "VerifyFull",
            _ => null
        };

        if (mapped is not null)
        {
            parts.Add($"SSL Mode={mapped}");
        }

        return string.Join(';', parts);
    }
}
