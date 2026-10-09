using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchedulingApp.Contracts;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Creates demo professionals and a demo client so a fresh deploy is immediately usable.</summary>
    public bool DemoData { get; set; }

    public string DemoPassword { get; set; } = "Demo@12345";
}

public static partial class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (db.Database.IsRelational())
        {
            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync(ct);
            }
            else
            {
                LogNoMigrations(logger);
                await db.Database.EnsureCreatedAsync(ct);
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { RoleNames.Client, RoleNames.Professional })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var seed = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (seed.DemoData)
            await SeedDemoDataAsync(provider, db, seed.DemoPassword, logger, ct);
    }

    private static async Task SeedDemoDataAsync(IServiceProvider provider, AppDbContext db, string password, ILogger logger, CancellationToken ct)
    {
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await users.FindByEmailAsync("client@demo.com") is not null)
            return;

        await CreateUserAsync(users, "client@demo.com", "Demo Client", RoleNames.Client, password);

        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        await CreateProfessionalAsync(users, db, password, ct,
            email: "sofia@demo.com", name: "Dr. Sofia Martins", specialty: "Dermatologist",
            bio: "Skin care consultations and follow-ups. 10+ years of clinical experience.",
            timeZone: "America/Sao_Paulo", slotMinutes: 30,
            hours: weekdays.SelectMany(d => new[] { (d, new TimeOnly(9, 0), new TimeOnly(12, 0)), (d, new TimeOnly(13, 0), new TimeOnly(18, 0)) }));

        await CreateProfessionalAsync(users, db, password, ct,
            email: "lucas@demo.com", name: "Lucas Oliveira", specialty: "Barber",
            bio: "Classic cuts, fades and beard trims.",
            timeZone: "America/Sao_Paulo", slotMinutes: 45,
            hours: new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                .Select(d => (d, new TimeOnly(10, 0), new TimeOnly(19, 0))));

        await CreateProfessionalAsync(users, db, password, ct,
            email: "emma@demo.com", name: "Emma Clarke", specialty: "English Tutor",
            bio: "Conversation and exam preparation classes (IELTS, TOEFL).",
            timeZone: "Europe/London", slotMinutes: 60,
            hours: Enum.GetValues<DayOfWeek>().SelectMany(d => new[] { (d, new TimeOnly(8, 0), new TimeOnly(12, 0)), (d, new TimeOnly(14, 0), new TimeOnly(20, 0)) }));

        LogDemoSeeded(logger);
    }

    private static async Task CreateProfessionalAsync(
        UserManager<ApplicationUser> users, AppDbContext db, string password, CancellationToken ct,
        string email, string name, string specialty, string bio, string timeZone, int slotMinutes,
        IEnumerable<(DayOfWeek Day, TimeOnly Start, TimeOnly End)> hours)
    {
        var user = await CreateUserAsync(users, email, name, RoleNames.Professional, password);
        var professional = new Professional
        {
            UserId = user.Id,
            DisplayName = name,
            Email = email,
            Specialty = specialty,
            Bio = bio,
            TimeZoneId = timeZone,
            SlotDurationMinutes = slotMinutes
        };

        foreach (var (day, start, end) in hours)
            professional.AvailabilityRules.Add(new AvailabilityRule { ProfessionalId = professional.Id, DayOfWeek = day, StartTime = start, EndTime = end });

        db.Professionals.Add(professional);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> users, string email, string name, string role, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(user, role);
        return user;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No EF Core migrations found; creating the schema with EnsureCreated. Run 'dotnet ef migrations add InitialCreate' for production.")]
    private static partial void LogNoMigrations(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data seeded (client@demo.com, sofia@demo.com, lucas@demo.com, emma@demo.com).")]
    private static partial void LogDemoSeeded(ILogger logger);
}
