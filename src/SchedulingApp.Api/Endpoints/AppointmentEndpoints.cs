using SchedulingApp.Api.Infrastructure;
using SchedulingApp.Application.Appointments;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Appointments;

namespace SchedulingApp.Api.Endpoints;

internal static class AppointmentEndpoints
{
    public static RouteGroupBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments").WithTags("Appointments").RequireAuthorization();

        group.MapPost("/", async (BookAppointmentRequest request, HttpContext http, AppointmentService service, CancellationToken ct) =>
            {
                var appointment = await service.BookAsync(http.User.ToActor(), request, ct);
                return TypedResults.Created($"/api/appointments/{appointment.Id}", appointment);
            })
            .RequireAuthorization(RoleNames.Client)
            .WithSummary("Request an appointment (client)");

        group.MapGet("/mine", async (HttpContext http, AppointmentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetForClientAsync(http.User.ToActor(), ct)))
            .RequireAuthorization(RoleNames.Client)
            .WithSummary("My appointments (client)");

        group.MapGet("/schedule", async (DateOnly? from, DateOnly? to, HttpContext http, AppointmentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetScheduleAsync(http.User.ToActor(), from, to, ct)))
            .RequireAuthorization(RoleNames.Professional)
            .WithSummary("Agenda for a date range in the professional's time zone");

        group.MapPost("/{id:guid}/confirm", async (Guid id, HttpContext http, AppointmentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ConfirmAsync(http.User.ToActor(), id, ct)))
            .RequireAuthorization(RoleNames.Professional)
            .WithSummary("Confirm a pending appointment (professional)");

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelAppointmentRequest? request, HttpContext http, AppointmentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.CancelAsync(http.User.ToActor(), id, request?.Reason, ct)))
            .WithSummary("Cancel (client) or decline/cancel (professional)");

        return group;
    }
}
