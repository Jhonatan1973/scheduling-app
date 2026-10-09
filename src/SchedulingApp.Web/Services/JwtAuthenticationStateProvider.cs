using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SchedulingApp.Domain.Constants;

namespace SchedulingApp.Web.Services;

public class JwtAuthenticationStateProvider(AuthSession session) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!session.IsAuthenticated || session.Current is null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, session.Current.FullName),
            new Claim(ClaimTypes.Email, session.Current.Email),
            new Claim(ClaimTypes.Role, session.Current.Role)
        ], "jwt");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
