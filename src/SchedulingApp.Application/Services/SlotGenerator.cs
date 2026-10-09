using SchedulingApp.Application.Dtos;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Services;

public static class SlotGenerator
{
    public static IReadOnlyList<TimeSlotDto> Generate(
        DateOnly date,
        Professional professional,
        IEnumerable<AvailabilityRule> rules,
        IEnumerable<Appointment> bookedAppointments)
    {
        var dayRules = rules.Where(r => r.DayOfWeek == date.DayOfWeek).ToList();
        if (dayRules.Count == 0)
        {
            return [];
        }

        var duration = TimeSpan.FromMinutes(Math.Max(professional.SlotDurationMinutes, 15));
        var slots = new List<TimeSlotDto>();

        foreach (var rule in dayRules)
        {
            var cursor = rule.StartTime;
            while (cursor.Add(duration) <= rule.EndTime)
            {
                var startLocal = date.ToDateTime(cursor, DateTimeKind.Unspecified);
                var endLocal = startLocal.Add(duration);
                var startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Utc);

                var overlaps = bookedAppointments.Any(a =>
                    a.Status != Domain.Enums.AppointmentStatus.Cancelled &&
                    startUtc < a.EndUtc &&
                    endUtc > a.StartUtc);

                slots.Add(new TimeSlotDto(startUtc, endUtc, !overlaps));
                cursor = cursor.Add(duration);
            }
        }

        return slots
            .Where(s => s.StartUtc >= DateTime.UtcNow)
            .OrderBy(s => s.StartUtc)
            .ToList();
    }
}
