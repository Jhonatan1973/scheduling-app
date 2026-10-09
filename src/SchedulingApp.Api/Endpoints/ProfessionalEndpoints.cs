using SchedulingApp.Api.Infrastructure;
using SchedulingApp.Application.Availability;
using SchedulingApp.Application.Professionals;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Availability;
using SchedulingApp.Contracts.Professionals;

namespace SchedulingApp.Api.Endpoints;

internal static class ProfessionalEndpoints
{
    public static RouteGroupBuilder MapProfessionalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professionals").WithTags("Professionals");

        // ---- Public ----
        group.MapGet("/", async (string? search, ProfessionalService service, CancellationToken ct) =>
                TypedResults.Ok(await service.SearchAsync(search, ct)))
            .WithSummary("List / search professionals");

        group.MapGet("/{id:guid}", async (Guid id, ProfessionalService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetAsync(id, ct)))
            .WithSummary("Professional details");

        group.MapGet("/{id:guid}/availability", async (Guid id, AvailabilityService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetRulesAsync(id, ct)))
            .WithSummary("Weekly working hours");

        group.MapGet("/{id:guid}/slots", async (Guid id, DateOnly date, AvailabilityService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetSlotsAsync(id, date, ct)))
            .WithSummary("Bookable slots for a local date (yyyy-MM-dd)");

        // ---- Professional self-service ----
        var me = group.MapGroup("/me").RequireAuthorization(RoleNames.Professional);

        me.MapGet("/", async (HttpContext http, ProfessionalService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetMineAsync(http.User.ToActor(), ct)))
            .WithSummary("My professional profile");

        me.MapPut("/", async (UpdateProfessionalProfileRequest request, HttpContext http, ProfessionalService service, CancellationToken ct) =>
                TypedResults.Ok(await service.UpdateMineAsync(http.User.ToActor(), request, ct)))
            .WithSummary("Update my profile (name, specialty, slot length, time zone)");

        me.MapGet("/availability", async (HttpContext http, AvailabilityService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetRulesAsync(http.User.ToActor().RequireProfessionalId(), ct)))
            .WithSummary("My weekly working hours");

        me.MapPost("/availability", async (CreateAvailabilityRuleRequest request, HttpContext http, AvailabilityService service, CancellationToken ct) =>
                TypedResults.Ok(await service.AddRuleAsync(http.User.ToActor(), request, ct)))
            .WithSummary("Add a working interval");

        me.MapDelete("/availability/{ruleId:guid}", async (Guid ruleId, HttpContext http, AvailabilityService service, CancellationToken ct) =>
            {
                await service.DeleteRuleAsync(http.User.ToActor(), ruleId, ct);
                return TypedResults.NoContent();
            })
            .WithSummary("Remove a working interval");

        return group;
    }
}
