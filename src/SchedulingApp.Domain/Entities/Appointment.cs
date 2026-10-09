using SchedulingApp.Domain.Common;
using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Domain.Entities;

public class Appointment
{
    public const int NotesMaxLength = 500;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalId { get; set; }
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public required string ClientEmail { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public UserRole? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public Professional? Professional { get; set; }

    public bool IsActive => Status != AppointmentStatus.Cancelled;

    public static Appointment Request(
        Guid professionalId,
        string clientId,
        string clientName,
        string clientEmail,
        DateTime startUtc,
        DateTime endUtc,
        string? notes,
        DateTime nowUtc)
    {
        if (startUtc <= nowUtc)
            throw new DomainException("appointment.in_past", "You cannot book a time in the past.");
        if (endUtc <= startUtc)
            throw new DomainException("appointment.invalid_range", "Appointment end must be after its start.");
        if (notes?.Length > NotesMaxLength)
            throw new DomainException("appointment.notes_too_long", $"Notes must be at most {NotesMaxLength} characters.");

        return new Appointment
        {
            ProfessionalId = professionalId,
            ClientId = clientId,
            ClientName = clientName,
            ClientEmail = clientEmail,
            StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = AppointmentStatus.Pending,
            CreatedAtUtc = nowUtc
        };
    }

    public void Confirm(DateTime nowUtc)
    {
        if (Status == AppointmentStatus.Confirmed)
            throw new DomainException("appointment.already_confirmed", "This appointment is already confirmed.");
        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("appointment.cancelled", "A cancelled appointment cannot be confirmed.");
        if (EndUtc <= nowUtc)
            throw new DomainException("appointment.finished", "This appointment is already in the past.");

        Status = AppointmentStatus.Confirmed;
        ConfirmedAtUtc = nowUtc;
    }

    public void Cancel(UserRole cancelledBy, string? reason, DateTime nowUtc)
    {
        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("appointment.already_cancelled", "This appointment is already cancelled.");
        if (StartUtc <= nowUtc)
            throw new DomainException("appointment.started", "Appointments that already started cannot be cancelled.");

        Status = AppointmentStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelledBy = cancelledBy;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
