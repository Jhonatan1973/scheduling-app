using SchedulingApp.Contracts.Auth;

namespace SchedulingApp.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}
