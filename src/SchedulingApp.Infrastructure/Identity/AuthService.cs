using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Common;
using SchedulingApp.Contracts;
using SchedulingApp.Contracts.Auth;
using SchedulingApp.Domain;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    JwtTokenService tokens) : IAuthService
{
    public const string DefaultTimeZone = "America/Sao_Paulo";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("auth.email_taken", "An account with this email already exists.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim()
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["password"] = created.Errors.Select(e => e.Description).ToArray()
            });
        }

        await EnsureSucceeded(userManager.AddToRoleAsync(user, request.Role.ToString()));

        Guid? professionalId = null;
        if (request.Role == UserRole.Professional)
        {
            var professional = new Professional
            {
                UserId = user.Id,
                DisplayName = user.FullName,
                Email = email,
                Specialty = request.Specialty!.Trim(),
                TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? DefaultTimeZone : request.TimeZoneId,
                SlotDurationMinutes = 30
            };
            db.Professionals.Add(professional);
            await db.SaveChangesAsync(cancellationToken);
            professionalId = professional.Id;
        }

        await transaction.CommitAsync(cancellationToken);
        return BuildResponse(user, request.Role, professionalId);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        const string invalid = "Invalid email or password.";

        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new UnauthorizedException(invalid);

        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("Too many failed attempts. Try again in a few minutes.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedException(invalid);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var (role, professionalId) = await GetRoleAsync(user, cancellationToken);
        return BuildResponse(user, role, professionalId);
    }

    public async Task<UserDto> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User");
        var (role, professionalId) = await GetRoleAsync(user, cancellationToken);
        return new UserDto(user.Id, user.FullName, user.Email ?? string.Empty, role, professionalId);
    }

    private async Task<(UserRole Role, Guid? ProfessionalId)> GetRoleAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(RoleNames.Professional))
            return (UserRole.Client, null);

        var professionalId = await db.Professionals
            .Where(p => p.UserId == user.Id)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        return (UserRole.Professional, professionalId);
    }

    private AuthResponse BuildResponse(ApplicationUser user, UserRole role, Guid? professionalId)
    {
        var (token, expires) = tokens.Create(user, role, professionalId);
        return new AuthResponse(token, expires, new UserDto(user.Id, user.FullName, user.Email ?? string.Empty, role, professionalId));
    }

    private static void Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length is < 2 or > 120)
            errors["fullName"] = ["Full name must have between 2 and 120 characters."];

        if (string.IsNullOrWhiteSpace(request.Email) || !MailAddress.TryCreate(request.Email.Trim(), out _))
            errors["email"] = ["A valid email is required."];

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
            errors["password"] = ["Password must have at least 8 characters."];

        if (!Enum.IsDefined(request.Role))
            errors["role"] = ["Role must be Client or Professional."];

        if (request.Role == UserRole.Professional)
        {
            if (string.IsNullOrWhiteSpace(request.Specialty) || request.Specialty.Trim().Length > 120)
                errors["specialty"] = ["Specialty is required for professionals (max 120 characters)."];

            if (!string.IsNullOrWhiteSpace(request.TimeZoneId) && !TimeZoneResolver.IsValid(request.TimeZoneId))
                errors["timeZoneId"] = [$"Unknown time zone '{request.TimeZoneId}'."];
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static async Task EnsureSucceeded(Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
