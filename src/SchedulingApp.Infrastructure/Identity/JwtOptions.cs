namespace SchedulingApp.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "scheduling-app";
    public string Audience { get; set; } = "scheduling-app";

    /// <summary>HMAC signing key. Must be at least 32 characters. Never commit a real value.</summary>
    public string Secret { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 480;
}
