using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Application.Dtos;

public record CreateAppointmentRequest(Guid ProfessionalId, DateTime StartUtc, string? Notes);
public record AppointmentDto(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    string ClientName,
    DateTime StartUtc,
    DateTime EndUtc,
    AppointmentStatus Status,
    string? Notes);

public record DashboardSummaryDto(int TodayCount, int WeekCount, IReadOnlyList<AppointmentDto> Upcoming);
