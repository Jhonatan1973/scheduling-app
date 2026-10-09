using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Auth;

namespace SchedulingApp.Web.Services;

/// <summary>Circuit-scoped authentication state backed by the JWT returned from the API.</summary>
public sealed class AppAuthStateProvider(SessionStore store) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private AuthResponse? _session;
    private Task? _initialization;

    public bool IsInitialized { get; private set; }

    public AuthResponse? Session => _session is { } s && s.ExpiresAtUtc > DateTime.UtcNow ? s : null;
    public UserDto? User => Session?.User;
    public bool IsAuthenticated => Session is not null;
    public bool IsClient => User?.Role == UserRole.Client;
    public bool IsProfessional => User?.Role == UserRole.Professional;

    /// <summary>Loads the stored session once per circuit (needs JS interop, so call it after the first render).</summary>
    public Task EnsureInitializedAsync() => _initialization ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
    {
        _session = await store.LoadAsync();
        IsInitialized = true;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (Session is not { } session)
            return Task.FromResult(Anonymous);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, session.User.Id),
            new Claim(ClaimTypes.Name, session.User.FullName),
            new Claim(ClaimTypes.Email, session.User.Email),
            new Claim(ClaimTypes.Role, session.User.Role.ToString())
        ], authenticationType: "jwt");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public async Task SignInAsync(AuthResponse session)
    {
        _session = session;
        IsInitialized = true;
        _initialization ??= Task.CompletedTask;
        await store.SaveAsync(session);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        _session = null;
        await store.ClearAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
