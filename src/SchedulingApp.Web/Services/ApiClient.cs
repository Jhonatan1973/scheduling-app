using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Appointments;
using SchedulingApp.Contracts.Auth;
using SchedulingApp.Contracts.Availability;
using SchedulingApp.Contracts.Professionals;

namespace SchedulingApp.Web.Services;

public sealed record ApiResult<T>(T? Value, string? Error, HttpStatusCode Status)
{
    public bool IsSuccess => Error is null;
}

/// <summary>Typed HTTP client for the Scheduling API. Never throws for HTTP errors: returns friendly messages instead.</summary>
public sealed class ApiClient(HttpClient http, AppAuthStateProvider auth)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // ---------- Auth ----------
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request);

    // ---------- Professionals (public) ----------
    public Task<ApiResult<List<ProfessionalDto>>> GetProfessionalsAsync(string? search = null) =>
        SendAsync<List<ProfessionalDto>>(HttpMethod.Get,
            string.IsNullOrWhiteSpace(search) ? "api/professionals" : $"api/professionals?search={Uri.EscapeDataString(search)}");

    public Task<ApiResult<ProfessionalDto>> GetProfessionalAsync(Guid id) =>
        SendAsync<ProfessionalDto>(HttpMethod.Get, $"api/professionals/{id}");

    public Task<ApiResult<List<AvailabilityRuleDto>>> GetAvailabilityAsync(Guid professionalId) =>
        SendAsync<List<AvailabilityRuleDto>>(HttpMethod.Get, $"api/professionals/{professionalId}/availability");

    public Task<ApiResult<DaySlotsDto>> GetSlotsAsync(Guid professionalId, DateOnly date) =>
        SendAsync<DaySlotsDto>(HttpMethod.Get, $"api/professionals/{professionalId}/slots?date={date:yyyy-MM-dd}");

    // ---------- Professional self-service ----------
    public Task<ApiResult<ProfessionalDto>> GetMyProfileAsync() =>
        SendAsync<ProfessionalDto>(HttpMethod.Get, "api/professionals/me");

    public Task<ApiResult<ProfessionalDto>> UpdateMyProfileAsync(UpdateProfessionalProfileRequest request) =>
        SendAsync<ProfessionalDto>(HttpMethod.Put, "api/professionals/me", request);

    public Task<ApiResult<List<AvailabilityRuleDto>>> GetMyAvailabilityAsync() =>
        SendAsync<List<AvailabilityRuleDto>>(HttpMethod.Get, "api/professionals/me/availability");

    public Task<ApiResult<AvailabilityRuleDto>> AddAvailabilityAsync(CreateAvailabilityRuleRequest request) =>
        SendAsync<AvailabilityRuleDto>(HttpMethod.Post, "api/professionals/me/availability", request);

    public Task<ApiResult<bool>> DeleteAvailabilityAsync(Guid ruleId) =>
        SendAsync<bool>(HttpMethod.Delete, $"api/professionals/me/availability/{ruleId}");

    // ---------- Appointments ----------
    public Task<ApiResult<AppointmentDto>> BookAsync(BookAppointmentRequest request) =>
        SendAsync<AppointmentDto>(HttpMethod.Post, "api/appointments", request);

    public Task<ApiResult<List<AppointmentDto>>> GetMyAppointmentsAsync() =>
        SendAsync<List<AppointmentDto>>(HttpMethod.Get, "api/appointments/mine");

    public Task<ApiResult<ScheduleDto>> GetScheduleAsync(DateOnly from, DateOnly to) =>
        SendAsync<ScheduleDto>(HttpMethod.Get, $"api/appointments/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<ApiResult<AppointmentDto>> ConfirmAsync(Guid appointmentId) =>
        SendAsync<AppointmentDto>(HttpMethod.Post, $"api/appointments/{appointmentId}/confirm");

    public Task<ApiResult<AppointmentDto>> CancelAsync(Guid appointmentId, string? reason) =>
        SendAsync<AppointmentDto>(HttpMethod.Post, $"api/appointments/{appointmentId}/cancel", new CancelAppointmentRequest(reason));

    // ---------- Plumbing ----------
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);

        if (auth.Session is { } session)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<T>(default, "The server is not reachable right now. Please try again in a moment.", HttpStatusCode.ServiceUnavailable);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(bool))
                    return new ApiResult<T>((T)(object)true, null, response.StatusCode);

                var value = await response.Content.ReadFromJsonAsync<T>(Json);
                return new ApiResult<T>(value, null, response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized && auth.IsAuthenticated)
                await auth.SignOutAsync();

            return new ApiResult<T>(default, await ReadErrorAsync(response), response.StatusCode);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(Json);
            if (problem is not null)
                return problem.Message;
        }
        catch (JsonException)
        {
            // not a ProblemDetails body
        }
        catch (NotSupportedException)
        {
            // empty or non-JSON content type
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "Not found.",
            HttpStatusCode.TooManyRequests => "Too many attempts. Please wait a minute.",
            _ => $"Request failed ({(int)response.StatusCode})."
        };
    }
}
