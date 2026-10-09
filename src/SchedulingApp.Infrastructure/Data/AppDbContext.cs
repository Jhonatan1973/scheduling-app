using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchedulingApp.Application.Services;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), ISchedulingDbContext
{
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Professional>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.Specialty).HasMaxLength(120);
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<AvailabilityRule>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Professional)
                .WithMany(x => x.AvailabilityRules)
                .HasForeignKey(x => x.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ProfessionalId, x.DayOfWeek }).IsUnique();
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientName).HasMaxLength(120);
            entity.Property(x => x.ClientEmail).HasMaxLength(256);
            entity.HasOne(x => x.Professional)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.ProfessionalId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ProfessionalId, x.StartUtc });
        });
    }
}
