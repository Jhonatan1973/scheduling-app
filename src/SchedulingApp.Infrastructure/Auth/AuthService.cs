using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchedulingApp.Application.Dtos;
using SchedulingApp.Application.Options;
using SchedulingApp.Domain.Constants;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Data;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var role = Roles.Client;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);

        if (role == Roles.Admin)
        {
            dbContext.Professionals.Add(new Professional
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = request.FullName,
                Specialty = "General",
                SlotDurationMinutes = 30
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await BuildTokenResponseAsync(user, role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var signIn = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Client;
        return await BuildTokenResponseAsync(user, role);
    }

    private async Task<AuthResponse> BuildTokenResponseAsync(ApplicationUser user, string role)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        if (role == Roles.Admin)
        {
            var professional = dbContext.Professionals.FirstOrDefault(p => p.UserId == user.Id);
            if (professional is not null)
            {
                claims.Add(new Claim("professional_id", professional.Id.ToString()));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse(tokenValue, user.Email ?? string.Empty, user.FullName, role, expires);
    }
}
