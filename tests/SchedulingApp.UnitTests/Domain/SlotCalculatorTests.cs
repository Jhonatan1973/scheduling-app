using SchedulingApp.Domain.Scheduling;
using static SchedulingApp.UnitTests.Support.Builders;

namespace SchedulingApp.UnitTests.Domain;

public class SlotCalculatorTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    // Monday, 2030-01-07
    private static readonly DateOnly Monday = new(2030, 1, 7);
    private static readonly DateTime LongAgo = Utc(2020, 1, 1);

    [Fact]
    public void Splits_interval_into_slots_of_the_configured_duration()
    {
        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Monday, 9, 12)], 30, TimeZoneInfo.Utc, [], LongAgo);

        Assert.Equal(6, slots.Count);
        Assert.Equal(Utc(2030, 1, 7, 9), slots[0].StartUtc);
        Assert.Equal(Utc(2030, 1, 7, 11, 30), slots[^1].StartUtc);
        Assert.All(slots, s => Assert.True(s.IsAvailable));
    }

    [Fact]
    public void Ignores_a_trailing_fragment_shorter_than_the_slot()
    {
        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Monday, 9, 10, endMinute: 40)], 45, TimeZoneInfo.Utc, [], LongAgo);

        Assert.Equal(2, slots.Count); // 09:00 and 09:45 (10:30 would end at 11:15)
    }

    [Fact]
    public void Converts_professional_local_time_to_utc()
    {
        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Monday, 9, 10)], 60, SaoPaulo, [], LongAgo);

        var slot = Assert.Single(slots);
        Assert.Equal(Utc(2030, 1, 7, 12), slot.StartUtc); // 09:00 in São Paulo (UTC-3)
    }

    [Fact]
    public void Returns_nothing_for_days_without_rules()
    {
        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Tuesday, 9, 12)], 30, TimeZoneInfo.Utc, [], LongAgo);

        Assert.Empty(slots);
    }

    [Fact]
    public void Marks_slots_overlapping_existing_appointments_as_unavailable()
    {
        var busy = new[] { new TimeRange(Utc(2030, 1, 7, 9, 30), Utc(2030, 1, 7, 10)) };

        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Monday, 9, 11)], 30, TimeZoneInfo.Utc, busy, LongAgo);

        Assert.Equal([true, false, true, true], slots.Select(s => s.IsAvailable).ToArray());
    }

    [Fact]
    public void Excludes_slots_that_already_started()
    {
        var now = Utc(2030, 1, 7, 10, 10);

        var slots = SlotCalculator.Generate(Monday, [Rule(DayOfWeek.Monday, 9, 12)], 30, TimeZoneInfo.Utc, [], now);

        Assert.Equal(Utc(2030, 1, 7, 10, 30), slots[0].StartUtc);
        Assert.Equal(3, slots.Count);
    }

    [Fact]
    public void Supports_several_intervals_per_day_ordered_by_time()
    {
        var rules = new[] { Rule(DayOfWeek.Monday, 14, 15), Rule(DayOfWeek.Monday, 9, 10) };

        var slots = SlotCalculator.Generate(Monday, rules, 60, TimeZoneInfo.Utc, [], LongAgo);

        Assert.Equal([9, 14], slots.Select(s => s.StartUtc.Hour).ToArray());
    }

    [Fact]
    public void Skips_local_times_that_do_not_exist_on_dst_spring_forward()
    {
        // 2030-03-10 is the DST change in New York: 02:00 jumps to 03:00 (a Sunday).
        var dstDay = new DateOnly(2030, 3, 10);

        var slots = SlotCalculator.Generate(dstDay, [Rule(DayOfWeek.Sunday, 1, 4)], 30, NewYork, [], LongAgo);

        Assert.Equal(4, slots.Count); // 01:00, 01:30, 03:00, 03:30
        Assert.Equal(slots.Count, slots.Select(s => s.StartUtc).Distinct().Count());
    }

    [Fact]
    public void Local_day_range_covers_exactly_24_hours_on_normal_days()
    {
        var range = SlotCalculator.LocalDayToUtcRange(Monday, SaoPaulo);

        Assert.Equal(Utc(2030, 1, 7, 3), range.StartUtc);
        Assert.Equal(Utc(2030, 1, 8, 3), range.EndUtc);
    }
}
