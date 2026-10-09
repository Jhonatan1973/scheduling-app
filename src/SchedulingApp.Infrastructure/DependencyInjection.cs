using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Options;
using SchedulingApp.Application.Services;
using SchedulingApp.Domain.Constants;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Auth;
using SchedulingApp.Infrastructure.Caching;
using SchedulingApp.Infrastructure.Data;
using SchedulingApp.Infrastructure.Email;
using SchedulingApp.Infrastructure.Identity;
using SchedulingApp.Infrastructure.Messaging;

namespace SchedulingApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        services.AddScoped<ISchedulingDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ISlotCache, RedisSlotCache>();
        services.AddScoped<SchedulingService>();
        services.AddScoped<AuthService>();
        services.AddSingleton<EmailSender>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq"));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<INotificationPublisher, MassTransitNotificationPublisher>();

        return services;
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Client, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@scheduling-app.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Dr. Ana Admin"
            };

            await userManager.CreateAsync(admin, "ChangeMe123!");
            await userManager.AddToRoleAsync(admin, Roles.Admin);

            var professional = new Professional
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                DisplayName = "Dr. Ana Admin",
                Specialty = "Dermatology",
                SlotDurationMinutes = 30
            };

            db.Professionals.Add(professional);

            db.AvailabilityRules.AddRange(
                new AvailabilityRule
                {
                    Id = Guid.NewGuid(),
                    ProfessionalId = professional.Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0)
                },
                new AvailabilityRule
                {
                    Id = Guid.NewGuid(),
                    ProfessionalId = professional.Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0)
                });

            await db.SaveChangesAsync();
        }
    }
}
