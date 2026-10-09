using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SchedulingApp.Contracts.Auth;

namespace SchedulingApp.Web.Services;

/// <summary>Persists the JWT session in the browser (encrypted with ASP.NET Core Data Protection).</summary>
public sealed class SessionStore(ProtectedLocalStorage storage)
{
    private const string Key = "scheduling-app.session";

    public async Task<AuthResponse?> LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<AuthResponse>(Key);
            return result.Success ? result.Value : null;
        }
        catch (CryptographicException)
        {
            // Data-protection keys rotated (e.g. container restart): drop the stale session.
            await ClearAsync();
            return null;
        }
        catch (InvalidOperationException)
        {
            // JS interop not available (prerendering). Treat as anonymous.
            return null;
        }
    }

    public ValueTask SaveAsync(AuthResponse session) => storage.SetAsync(Key, session);

    public async Task ClearAsync()
    {
        try
        {
            await storage.DeleteAsync(Key);
        }
        catch (InvalidOperationException)
        {
            // ignored: no JS runtime yet
        }
    }
}
