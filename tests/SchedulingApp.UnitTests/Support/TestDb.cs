using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.UnitTests.Support;

/// <summary>EF Core InMemory context implementing the application persistence port.</summary>
internal sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options), IAppDbContext
{
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();

    public static TestDb Create() =>
        new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
