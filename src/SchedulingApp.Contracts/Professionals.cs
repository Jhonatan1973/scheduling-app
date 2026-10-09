namespace SchedulingApp.Contracts.Professionals;

public sealed record ProfessionalDto(
    Guid Id,
    string DisplayName,
    string Specialty,
    string? Bio,
    int SlotDurationMinutes,
    string TimeZoneId);

public sealed record UpdateProfessionalProfileRequest(
    string DisplayName,
    string Specialty,
    string? Bio,
    int SlotDurationMinutes,
    string TimeZoneId);
