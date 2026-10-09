namespace SchedulingApp.Contracts.Auth;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    string? Specialty = null,
    string? TimeZoneId = null);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(string Id, string FullName, string Email, UserRole Role, Guid? ProfessionalId);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
