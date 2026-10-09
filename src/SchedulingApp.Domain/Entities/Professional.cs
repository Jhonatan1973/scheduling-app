namespace SchedulingApp.Domain.Entities;

public class Professional
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }
    public required string Specialty { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;

    public ICollection<AvailabilityRule> AvailabilityRules { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
