namespace SchedulingApp.Domain;

public static class TimeZoneResolver
{
    /// <summary>Resolves IANA (and Windows) ids on any OS. Falls back to UTC for unknown ids.</summary>
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz) ? tz : TimeZoneInfo.Utc;
    }

    public static bool IsValid(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
