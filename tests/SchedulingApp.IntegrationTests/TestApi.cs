using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Auth;
using SchedulingApp.Contracts.Availability;

namespace SchedulingApp.IntegrationTests;

internal static class TestApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";

    public static async Task<(HttpClient Client, AuthResponse Auth)> RegisterAsync(
        ApiFactory factory, UserRole role, string? name = null)
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest(
            name ?? $"{role} User",
            UniqueEmail(role.ToString().ToLowerInvariant()),
            "Passw0rd!",
            role,
            role == UserRole.Professional ? "Barber" : null,
            role == UserRole.Professional ? "UTC" : null);

        var response = await client.PostAsJsonAsync("api/auth/register", request, Json);
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    /// <summary>Gives the professional working hours on a date one week ahead and returns that date.</summary>
    public static async Task<DateOnly> OpenDayAsync(HttpClient professional, int startHour = 9, int endHour = 12)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var response = await professional.PostAsJsonAsync(
            "api/professionals/me/availability",
            new CreateAvailabilityRuleRequest(date.DayOfWeek, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)),
            Json);
        response.EnsureSuccessStatusCode();
        return date;
    }

    public static DateTimeOffset At(DateOnly date, int hour, int minute = 0) =>
        new(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
