namespace SchedulingApp.Web.Services;

public static class TimeZones
{
    /// <summary>Curated IANA zones offered in the professional settings.</summary>
    public static readonly IReadOnlyList<string> Common =
    [
        "America/Sao_Paulo", "America/Manaus", "America/Fortaleza", "America/Bahia", "America/Cuiaba", "America/Rio_Branco",
        "America/Noronha", "America/Argentina/Buenos_Aires", "America/Santiago", "America/Bogota", "America/Mexico_City",
        "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles", "America/Toronto",
        "Europe/London", "Europe/Lisbon", "Europe/Madrid", "Europe/Paris", "Europe/Berlin",
        "Africa/Johannesburg", "Asia/Dubai", "Asia/Kolkata", "Asia/Tokyo", "Australia/Sydney", "UTC"
    ];

    public static TimeZoneInfo Resolve(string id) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? tz : TimeZoneInfo.Utc;

    public static DateOnly Today(string timeZoneId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Resolve(timeZoneId)));

    public static string Friendly(string id) => id.Replace('_', ' ');
}
