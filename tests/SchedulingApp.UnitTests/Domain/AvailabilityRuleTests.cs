using SchedulingApp.Domain.Common;
using SchedulingApp.Domain.Entities;
using static SchedulingApp.UnitTests.Support.Builders;

namespace SchedulingApp.UnitTests.Domain;

public class AvailabilityRuleTests
{
    private static readonly Guid ProfessionalId = Guid.NewGuid();

    [Fact]
    public void Creates_a_valid_interval()
    {
        var rule = AvailabilityRule.Create(ProfessionalId, DayOfWeek.Monday, new(9, 0), new(12, 0), []);

        Assert.Equal(DayOfWeek.Monday, rule.DayOfWeek);
        Assert.Equal(ProfessionalId, rule.ProfessionalId);
    }

    [Theory]
    [InlineData(12, 9)]
    [InlineData(9, 9)]
    public void Rejects_end_before_or_equal_start(int start, int end)
    {
        var ex = Assert.Throws<DomainException>(() =>
            AvailabilityRule.Create(ProfessionalId, DayOfWeek.Monday, new(start, 0), new(end, 0), []));

        Assert.Equal("availability.invalid_range", ex.Code);
    }

    [Theory]
    [InlineData(8, 10)]
    [InlineData(11, 13)]
    [InlineData(10, 11)]
    [InlineData(8, 14)]
    public void Rejects_overlapping_intervals_on_the_same_day(int start, int end)
    {
        var existing = new[] { Rule(DayOfWeek.Monday, 9, 12) };

        var ex = Assert.Throws<DomainException>(() =>
            AvailabilityRule.Create(ProfessionalId, DayOfWeek.Monday, new(start, 0), new(end, 0), existing));

        Assert.Equal("availability.overlap", ex.Code);
    }

    [Fact]
    public void Accepts_adjacent_intervals_and_other_days()
    {
        var existing = new[] { Rule(DayOfWeek.Monday, 9, 12) };

        AvailabilityRule.Create(ProfessionalId, DayOfWeek.Monday, new(12, 0), new(13, 0), existing);
        AvailabilityRule.Create(ProfessionalId, DayOfWeek.Tuesday, new(9, 0), new(12, 0), existing);
    }
}
