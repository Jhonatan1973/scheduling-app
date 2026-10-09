using SchedulingApp.Api.Infrastructure;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Contracts.Auth;

namespace SchedulingApp.Api.Endpoints;

internal static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth, CancellationToken ct) =>
                TypedResults.Ok(await auth.RegisterAsync(request, ct)))
            .RequireRateLimiting("auth")
            .WithSummary("Create a client or professional account");

        group.MapPost("/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
                TypedResults.Ok(await auth.LoginAsync(request, ct)))
            .RequireRateLimiting("auth")
            .WithSummary("Exchange credentials for a JWT");

        group.MapGet("/me", async (HttpContext http, IAuthService auth, CancellationToken ct) =>
                TypedResults.Ok(await auth.GetUserAsync(http.User.ToActor().UserId, ct)))
            .RequireAuthorization()
            .WithSummary("Current user profile");

        return group;
    }
}
