using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public required string ClientUserId { get; set; }
    public required string ClientName { get; set; }
    public required string ClientEmail { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Professional? Professional { get; set; }
}
