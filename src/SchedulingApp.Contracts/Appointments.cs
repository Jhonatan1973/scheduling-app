namespace SchedulingApp.Contracts.Appointments;

public sealed record BookAppointmentRequest(Guid ProfessionalId, DateTimeOffset Start, string? Notes);

public sealed record CancelAppointmentRequest(string? Reason);

public sealed record AppointmentDto(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    string ProfessionalSpecialty,
    string ClientName,
    string ClientEmail,
    DateTimeOffset Start,
    DateTimeOffset End,
    string TimeZoneId,
    AppointmentStatus Status,
    string? Notes,
    UserRole? CancelledBy,
    string? CancellationReason,
    DateTime CreatedAtUtc);

public sealed record ScheduleDto(
    DateOnly From,
    DateOnly To,
    string TimeZoneId,
    int PendingCount,
    int ConfirmedCount,
    int CancelledCount,
    IReadOnlyList<AppointmentDto> Appointments);
