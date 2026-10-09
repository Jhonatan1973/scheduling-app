using SchedulingApp.Domain;
using SchedulingApp.Domain.Entities;
using C = SchedulingApp.Contracts;
using SchedulingApp.Contracts.Appointments;
using SchedulingApp.Contracts.Availability;
using SchedulingApp.Contracts.Professionals;

namespace SchedulingApp.Application.Common;

internal static class Mapping
{
    public static DateTimeOffset ToZoned(this DateTime utc, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)), timeZone);

    public static ProfessionalDto ToDto(this Professional p) =>
        new(p.Id, p.DisplayName, p.Specialty, p.Bio, p.SlotDurationMinutes, p.TimeZoneId);

    public static AvailabilityRuleDto ToDto(this AvailabilityRule r) =>
        new(r.Id, r.DayOfWeek, r.StartTime, r.EndTime);

    public static AppointmentDto ToDto(this Appointment a, Professional professional)
    {
        var tz = TimeZoneResolver.Resolve(professional.TimeZoneId);
        return new AppointmentDto(
            a.Id,
            a.ProfessionalId,
            professional.DisplayName,
            professional.Specialty,
            a.ClientName,
            a.ClientEmail,
            a.StartUtc.ToZoned(tz),
            a.EndUtc.ToZoned(tz),
            professional.TimeZoneId,
            (C.AppointmentStatus)(int)a.Status,
            a.Notes,
            a.CancelledBy is { } by ? (C.UserRole)(int)by : null,
            a.CancellationReason,
            a.CreatedAtUtc);
    }
}
