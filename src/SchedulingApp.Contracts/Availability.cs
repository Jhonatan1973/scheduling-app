namespace SchedulingApp.Contracts.Availability;

public sealed record AvailabilityRuleDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public sealed record CreateAvailabilityRuleRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>Start/End carry the professional's UTC offset, so they display as the professional's local time.</summary>
public sealed record SlotDto(DateTimeOffset Start, DateTimeOffset End, bool IsAvailable);

public sealed record DaySlotsDto(DateOnly Date, string TimeZoneId, IReadOnlyList<SlotDto> Slots);
