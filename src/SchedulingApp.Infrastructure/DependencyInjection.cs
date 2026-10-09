using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Notifications;
using SchedulingApp.Contracts;
using SchedulingApp.Infrastructure.Email;
using SchedulingApp.Infrastructure.Identity;
using SchedulingApp.Infrastructure.Persistence;

namespace SchedulingApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(ConnectionStringResolver.Normalize(connectionString)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        AddEmail(services, configuration);
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateKey(jwt.Secret),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(RoleNames.Client, p => p.RequireRole(RoleNames.Client))
            .AddPolicy(RoleNames.Professional, p => p.RequireRole(RoleNames.Professional));

        return services;
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>($"{EmailOptions.SectionName}:Provider") ?? "Log";

        switch (provider.ToLowerInvariant())
        {
            case "smtp":
                services.AddTransient<IEmailSender, SmtpEmailSender>();
                break;
            case "brevo":
                services.AddHttpClient<IEmailSender, BrevoEmailSender>((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
                    client.BaseAddress = new Uri(options.Brevo.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(15);
                });
                break;
            default:
                services.AddTransient<IEmailSender, LogEmailSender>();
                break;
        }

        services.AddSingleton<OutboxEmailDispatcher>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxEmailDispatcher>());
    }
}
