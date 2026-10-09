namespace SchedulingApp.Domain.Entities;

public class AvailabilityRule
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public Professional? Professional { get; set; }
}
