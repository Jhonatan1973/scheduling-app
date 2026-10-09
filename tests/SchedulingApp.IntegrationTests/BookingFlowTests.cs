using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Appointments;
using SchedulingApp.Contracts.Availability;
using SchedulingApp.Infrastructure.Email;
using static SchedulingApp.IntegrationTests.TestApi;

namespace SchedulingApp.IntegrationTests;

[Collection(ApiCollection.Name)]
public class BookingFlowTests(ApiFactory factory)
{
    [IntegrationFact]
    public async Task Full_flow_book_confirm_cancel_with_emails()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional, "Flow Pro");
        var (client, clientAuth) = await RegisterAsync(factory, UserRole.Client, "Flow Client");
        var professionalId = proAuth.User.ProfessionalId!.Value;
        var date = await OpenDayAsync(pro);

        // 1. Client sees free slots
        var slots = await client.GetFromJsonAsync<DaySlotsDto>($"api/professionals/{professionalId}/slots?date={date:yyyy-MM-dd}", Json);
        Assert.Equal(6, slots!.Slots.Count); // 09:00-12:00, 30 min
        Assert.All(slots.Slots, s => Assert.True(s.IsAvailable));

        // 2. Client books 10:00
        var book = await client.PostAsJsonAsync("api/appointments",
            new BookAppointmentRequest(professionalId, At(date, 10), "Beard trim"), Json);
        Assert.Equal(HttpStatusCode.Created, book.StatusCode);
        var appointment = await book.ReadAsync<AppointmentDto>();
        Assert.Equal(AppointmentStatus.Pending, appointment.Status);

        // 3. Slot is now taken
        slots = await client.GetFromJsonAsync<DaySlotsDto>($"api/professionals/{professionalId}/slots?date={date:yyyy-MM-dd}", Json);
        Assert.False(slots!.Slots.Single(s => s.Start == At(date, 10)).IsAvailable);

        // 4. Professional sees it in the schedule and confirms
        var schedule = await pro.GetFromJsonAsync<ScheduleDto>($"api/appointments/schedule?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}", Json);
        Assert.Equal(1, schedule!.PendingCount);

        var confirm = await pro.PostAsync($"api/appointments/{appointment.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(AppointmentStatus.Confirmed, (await confirm.ReadAsync<AppointmentDto>()).Status);

        // 5. Client cancels
        var cancel = await client.PostAsJsonAsync($"api/appointments/{appointment.Id}/cancel", new CancelAppointmentRequest("Travelling"), Json);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var mine = await client.GetFromJsonAsync<List<AppointmentDto>>("api/appointments/mine", Json);
        Assert.Equal(AppointmentStatus.Cancelled, Assert.Single(mine!).Status);

        // 6. Outbox delivers: request (client + pro), confirmation (client), cancellation (pro)
        var dispatcher = factory.Services.GetRequiredService<OutboxEmailDispatcher>();
        await dispatcher.DispatchPendingAsync();

        var flowEmails = factory.Emails.Sent
            .Where(e => e.ToEmail == clientAuth.User.Email || e.ToEmail == proAuth.User.Email)
            .ToList();
        Assert.Equal(4, flowEmails.Count);
        Assert.Contains(flowEmails, e => e.ToEmail == clientAuth.User.Email && e.Subject.StartsWith("Appointment confirmed"));
        Assert.Contains(flowEmails, e => e.ToEmail == proAuth.User.Email && e.TextBody.Contains("Travelling"));
    }

    [IntegrationFact]
    public async Task Same_slot_cannot_be_booked_twice()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional);
        var (first, _) = await RegisterAsync(factory, UserRole.Client);
        var (second, _) = await RegisterAsync(factory, UserRole.Client);
        var date = await OpenDayAsync(pro);
        var request = new BookAppointmentRequest(proAuth.User.ProfessionalId!.Value, At(date, 9), null);

        var ok = await first.PostAsJsonAsync("api/appointments", request, Json);
        var conflict = await second.PostAsJsonAsync("api/appointments", request, Json);

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [IntegrationFact]
    public async Task Concurrent_requests_for_the_same_slot_produce_exactly_one_booking()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional);
        var date = await OpenDayAsync(pro);
        var request = new BookAppointmentRequest(proAuth.User.ProfessionalId!.Value, At(date, 11), null);

        var clients = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => RegisterAsync(factory, UserRole.Client)));
        var responses = await Task.WhenAll(clients.Select(c => c.Client.PostAsJsonAsync("api/appointments", request, Json)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [IntegrationFact]
    public async Task Cannot_book_outside_working_hours()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional);
        var (client, _) = await RegisterAsync(factory, UserRole.Client);
        var date = await OpenDayAsync(pro);

        var response = await client.PostAsJsonAsync("api/appointments",
            new BookAppointmentRequest(proAuth.User.ProfessionalId!.Value, At(date, 15), null), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [IntegrationFact]
    public async Task Roles_are_enforced()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional);
        var (client, _) = await RegisterAsync(factory, UserRole.Client);
        var date = await OpenDayAsync(pro);

        var proBooking = await pro.PostAsJsonAsync("api/appointments",
            new BookAppointmentRequest(proAuth.User.ProfessionalId!.Value, At(date, 9), null), Json);
        var clientSchedule = await client.GetAsync("api/appointments/schedule");
        var clientAvailability = await client.PostAsJsonAsync("api/professionals/me/availability",
            new CreateAvailabilityRuleRequest(DayOfWeek.Monday, new(9, 0), new(10, 0)), Json);

        Assert.Equal(HttpStatusCode.Forbidden, proBooking.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clientSchedule.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, clientAvailability.StatusCode);
    }

    [IntegrationFact]
    public async Task Professionals_cannot_touch_other_professionals_appointments()
    {
        var (pro, proAuth) = await RegisterAsync(factory, UserRole.Professional);
        var (otherPro, _) = await RegisterAsync(factory, UserRole.Professional);
        var (client, _) = await RegisterAsync(factory, UserRole.Client);
        var date = await OpenDayAsync(pro);

        var book = await client.PostAsJsonAsync("api/appointments",
            new BookAppointmentRequest(proAuth.User.ProfessionalId!.Value, At(date, 9, 30), null), Json);
        var appointment = await book.ReadAsync<AppointmentDto>();

        var confirm = await otherPro.PostAsync($"api/appointments/{appointment.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, confirm.StatusCode);
    }

    [IntegrationFact]
    public async Task Overlapping_availability_is_rejected()
    {
        var (pro, _) = await RegisterAsync(factory, UserRole.Professional);
        await OpenDayAsync(pro, 9, 12);

        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var overlap = await pro.PostAsJsonAsync("api/professionals/me/availability",
            new CreateAvailabilityRuleRequest(date.DayOfWeek, new(11, 0), new(13, 0)), Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, overlap.StatusCode);
    }
}
