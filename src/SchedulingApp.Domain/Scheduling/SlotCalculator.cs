using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Domain.Scheduling;

public readonly record struct TimeRange(DateTime StartUtc, DateTime EndUtc)
{
    public bool Overlaps(TimeRange other) => StartUtc < other.EndUtc && EndUtc > other.StartUtc;
}

public readonly record struct Slot(DateTime StartUtc, DateTime EndUtc, bool IsAvailable);

/// <summary>
/// Turns weekly availability rules (local time) into concrete UTC slots for a given local date.
/// Pure function: easy to unit test and independent of persistence.
/// </summary>
public static class SlotCalculator
{
    public static IReadOnlyList<Slot> Generate(
        DateOnly localDate,
        IEnumerable<AvailabilityRule> rules,
        int slotDurationMinutes,
        TimeZoneInfo timeZone,
        IEnumerable<TimeRange> busy,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(slotDurationMinutes, Professional.MinSlotMinutes);

        var duration = TimeSpan.FromMinutes(slotDurationMinutes);
        var busyRanges = busy.ToList();
        var slots = new List<Slot>();

        foreach (var rule in rules.Where(r => r.DayOfWeek == localDate.DayOfWeek).OrderBy(r => r.StartTime))
        {
            var cursor = localDate.ToDateTime(rule.StartTime, DateTimeKind.Unspecified);
            var ruleEnd = localDate.ToDateTime(rule.EndTime, DateTimeKind.Unspecified);

            while (cursor + duration <= ruleEnd)
            {
                var localStart = cursor;
                cursor += duration;

                // Skip local times that do not exist (DST "spring forward" gap).
                if (timeZone.IsInvalidTime(localStart))
                    continue;

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
                var endUtc = startUtc + duration;

                if (startUtc <= nowUtc)
                    continue;

                var range = new TimeRange(startUtc, endUtc);
                var isFree = !busyRanges.Any(b => b.Overlaps(range));
                slots.Add(new Slot(startUtc, endUtc, isFree));
            }
        }

        return slots.OrderBy(s => s.StartUtc).ToList();
    }

    /// <summary>UTC window [start, end) that covers a whole local day in the given zone.</summary>
    public static TimeRange LocalDayToUtcRange(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var start = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var end = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new TimeRange(ToUtcSafe(start, timeZone), ToUtcSafe(end, timeZone));
    }

    private static DateTime ToUtcSafe(DateTime local, TimeZoneInfo timeZone)
    {
        // Midnight can be invalid in zones that switch DST at 00:00; move forward until valid.
        while (timeZone.IsInvalidTime(local))
            local = local.AddMinutes(30);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
