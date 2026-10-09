using SchedulingApp.Application.Dtos;
using SchedulingApp.Domain.Constants;

namespace SchedulingApp.Web.Services;

public class AuthSession
{
    public AuthResponse? Current { get; private set; }

    public bool IsAuthenticated => Current is not null && Current.ExpiresAtUtc > DateTime.UtcNow;

    public bool IsAdmin => Current?.Role == Roles.Admin;

    public void Set(AuthResponse response) => Current = response;

    public void Clear() => Current = null;
}
