using SchedulingApp.Domain.Enums;

namespace SchedulingApp.Application.Common;

/// <summary>The authenticated user executing a use case (built from JWT claims by the API).</summary>
public sealed record Actor(string UserId, string Name, string Email, UserRole Role, Guid? ProfessionalId)
{
    public Guid RequireProfessionalId() =>
        Role == UserRole.Professional && ProfessionalId is { } id
            ? id
            : throw new ForbiddenException("Only professionals can perform this action.");

    public void RequireClient()
    {
        if (Role != UserRole.Client)
            throw new ForbiddenException("Only clients can perform this action.");
    }
}
