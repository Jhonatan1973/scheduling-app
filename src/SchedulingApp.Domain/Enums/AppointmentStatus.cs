namespace SchedulingApp.Domain.Enums;

public enum AppointmentStatus
{
    /// <summary>Requested by the client, waiting for the professional.</summary>
    Pending = 0,

    /// <summary>Accepted by the professional.</summary>
    Confirmed = 1,

    /// <summary>Cancelled by the client or declined/cancelled by the professional.</summary>
    Cancelled = 2
}
