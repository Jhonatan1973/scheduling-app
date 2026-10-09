using SchedulingApp.Domain.Common;

namespace SchedulingApp.Domain.Entities;

public class Professional
{
    public const int MinSlotMinutes = 10;
    public const int MaxSlotMinutes = 240;

    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public required string DisplayName { get; set; }

    /// <summary>Where booking notifications are sent.</summary>
    public required string Email { get; set; }
    public required string Specialty { get; set; }
    public string? Bio { get; set; }

    /// <summary>IANA time zone id (e.g. "America/Sao_Paulo"). Availability is expressed in this zone.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    public int SlotDurationMinutes { get; set; } = 30;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<AvailabilityRule> AvailabilityRules { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];

    public TimeZoneInfo GetTimeZone() => TimeZoneResolver.Resolve(TimeZoneId);

    public void UpdateProfile(string displayName, string specialty, string? bio, int slotDurationMinutes, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("professional.name_required", "Display name is required.");
        if (string.IsNullOrWhiteSpace(specialty))
            throw new DomainException("professional.specialty_required", "Specialty is required.");
        if (slotDurationMinutes is < MinSlotMinutes or > MaxSlotMinutes)
            throw new DomainException("professional.invalid_slot_duration",
                $"Slot duration must be between {MinSlotMinutes} and {MaxSlotMinutes} minutes.");
        if (!TimeZoneResolver.IsValid(timeZoneId))
            throw new DomainException("professional.invalid_time_zone", $"Unknown time zone '{timeZoneId}'.");

        DisplayName = displayName.Trim();
        Specialty = specialty.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        SlotDurationMinutes = slotDurationMinutes;
        TimeZoneId = timeZoneId;
    }
}
