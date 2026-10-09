namespace SchedulingApp.Application.Dtos;

public record AvailabilityRuleDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record UpsertAvailabilityRuleRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record TimeSlotDto(DateTime StartUtc, DateTime EndUtc, bool IsAvailable);
