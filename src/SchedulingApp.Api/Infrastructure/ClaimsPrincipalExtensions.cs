using System.Security.Claims;
using SchedulingApp.Application.Common;
using SchedulingApp.Domain.Enums;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Api.Infrastructure;

internal static class ClaimsPrincipalExtensions
{
    public static Actor ToActor(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(AppClaims.Subject)
            ?? throw new UnauthorizedException("Missing subject claim.");

        var role = Enum.TryParse<UserRole>(user.FindFirstValue(AppClaims.Role), out var parsed)
            ? parsed
            : UserRole.Client;

        Guid? professionalId = Guid.TryParse(user.FindFirstValue(AppClaims.ProfessionalId), out var id) ? id : null;

        return new Actor(
            userId,
            user.FindFirstValue(AppClaims.Name) ?? "User",
            user.FindFirstValue(AppClaims.Email) ?? string.Empty,
            role,
            professionalId);
    }
}
