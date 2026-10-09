namespace SchedulingApp.Contracts;

public enum UserRole
{
    Client = 0,
    Professional = 1
}

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

public static class RoleNames
{
    public const string Client = nameof(UserRole.Client);
    public const string Professional = nameof(UserRole.Professional);
}
