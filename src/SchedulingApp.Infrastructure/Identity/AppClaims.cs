namespace SchedulingApp.Infrastructure.Identity;

/// <summary>Short JWT claim names (inbound claim mapping is disabled in the API).</summary>
public static class AppClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string ProfessionalId = "professional_id";
}
