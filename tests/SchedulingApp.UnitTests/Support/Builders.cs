using SchedulingApp.Domain.Entities;

namespace SchedulingApp.UnitTests.Support;

internal static class Builders
{
    public static AvailabilityRule Rule(DayOfWeek day, int startHour, int endHour, int startMinute = 0, int endMinute = 0) => new()
    {
        DayOfWeek = day,
        StartTime = new TimeOnly(startHour, startMinute),
        EndTime = new TimeOnly(endHour, endMinute)
    };

    public static Professional Professional(string timeZone = "America/Sao_Paulo", int slotMinutes = 30) => new()
    {
        UserId = Guid.NewGuid().ToString(),
        DisplayName = "Dr. Test",
        Email = "pro@test.com",
        Specialty = "Testing",
        TimeZoneId = timeZone,
        SlotDurationMinutes = slotMinutes
    };

    public static Appointment Appointment(DateTime startUtc, int minutes = 30, DateTime? nowUtc = null) =>
        global::SchedulingApp.Domain.Entities.Appointment.Request(
            Guid.NewGuid(), "client-1", "Client", "client@test.com",
            startUtc, startUtc.AddMinutes(minutes), null, nowUtc ?? startUtc.AddDays(-1));

    public static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);
}
