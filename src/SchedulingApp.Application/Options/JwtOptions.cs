namespace SchedulingApp.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string Secret { get; set; }
    public int ExpirationMinutes { get; set; } = 480;
}
