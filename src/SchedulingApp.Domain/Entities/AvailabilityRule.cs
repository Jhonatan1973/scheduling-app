using SchedulingApp.Domain.Common;

namespace SchedulingApp.Domain.Entities;

/// <summary>
/// A weekly working interval in the professional's local time, e.g. Monday 09:00-12:00.
/// A day may have several non-overlapping intervals (morning / afternoon).
/// </summary>
public class AvailabilityRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public Professional? Professional { get; set; }

    public static AvailabilityRule Create(
        Guid professionalId,
        DayOfWeek dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        IEnumerable<AvailabilityRule> existingRules)
    {
        if (end <= start)
            throw new DomainException("availability.invalid_range", "End time must be after start time.");

        var overlapping = existingRules.Any(r =>
            r.DayOfWeek == dayOfWeek && start < r.EndTime && end > r.StartTime);

        if (overlapping)
            throw new DomainException("availability.overlap", $"This interval overlaps another {dayOfWeek} interval.");

        return new AvailabilityRule
        {
            ProfessionalId = professionalId,
            DayOfWeek = dayOfWeek,
            StartTime = start,
            EndTime = end
        };
    }
}
