using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Infrastructure.Persistence.Configurations;

internal sealed class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("Professionals");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Specialty).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Bio).HasMaxLength(1000);
        builder.Property(p => p.TimeZoneId).HasMaxLength(64).IsRequired();

        builder.HasIndex(p => p.UserId).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Professional>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.AvailabilityRules)
            .WithOne(r => r.Professional)
            .HasForeignKey(r => r.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Professional)
            .HasForeignKey(a => a.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
