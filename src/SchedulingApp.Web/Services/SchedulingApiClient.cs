using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchedulingApp.Application.Dtos;

namespace SchedulingApp.Web.Services;

public class SchedulingApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("SchedulingApi");
        if (session.IsAuthenticated)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Current!.Token);
        }

        return client;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await CreateClient().PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await CreateClient().PostAsJsonAsync("api/auth/register", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public async Task<IReadOnlyList<ProfessionalDto>> GetProfessionalsAsync()
    {
        var items = await CreateClient().GetFromJsonAsync<List<ProfessionalDto>>("api/professionals");
        return items ?? [];
    }

    public async Task<ProfessionalDto?> GetMyProfessionalAsync()
    {
        return await CreateClient().GetFromJsonAsync<ProfessionalDto>("api/professionals/me");
    }

    public async Task<IReadOnlyList<TimeSlotDto>> GetSlotsAsync(Guid professionalId, DateOnly date)
    {
        var items = await CreateClient().GetFromJsonAsync<List<TimeSlotDto>>(
            $"api/availability/{professionalId}/slots?date={date:yyyy-MM-dd}");
        return items ?? [];
    }

    public async Task<IReadOnlyList<AvailabilityRuleDto>> GetAvailabilityRulesAsync(Guid professionalId)
    {
        var items = await CreateClient().GetFromJsonAsync<List<AvailabilityRuleDto>>(
            $"api/availability/{professionalId}/rules");
        return items ?? [];
    }

    public async Task<bool> UpsertAvailabilityRuleAsync(UpsertAvailabilityRuleRequest request)
    {
        var response = await CreateClient().PutAsJsonAsync("api/availability/rules", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAvailabilityRuleAsync(Guid ruleId)
    {
        var response = await CreateClient().DeleteAsync($"api/availability/rules/{ruleId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<AppointmentDto?> BookAsync(CreateAppointmentRequest request)
    {
        var response = await CreateClient().PostAsJsonAsync("api/appointments", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AppointmentDto>();
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetMyAppointmentsAsync()
    {
        var items = await CreateClient().GetFromJsonAsync<List<AppointmentDto>>("api/appointments/mine");
        return items ?? [];
    }

    public async Task<DashboardSummaryDto?> GetDashboardAsync()
    {
        return await CreateClient().GetFromJsonAsync<DashboardSummaryDto>("api/appointments/dashboard");
    }

    public async Task<bool> CancelAppointmentAsync(Guid appointmentId)
    {
        var response = await CreateClient().PostAsync($"api/appointments/{appointmentId}/cancel", null);
        return response.IsSuccessStatusCode;
    }
}
