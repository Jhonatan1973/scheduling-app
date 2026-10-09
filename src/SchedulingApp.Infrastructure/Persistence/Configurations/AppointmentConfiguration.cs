using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchedulingApp.Domain.Entities;
using SchedulingApp.Domain.Enums;
using SchedulingApp.Infrastructure.Identity;

namespace SchedulingApp.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ClientName).HasMaxLength(120).IsRequired();
        builder.Property(a => a.ClientEmail).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(Appointment.NotesMaxLength);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Ignore(a => a.IsActive);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ClientId);

        // Database-level guarantee against double booking: only one active appointment
        // may start at a given time for a professional (cancelled rows are ignored).
        builder.HasIndex(a => new { a.ProfessionalId, a.StartUtc })
            .IsUnique()
            .HasFilter($"\"Status\" <> {(int)AppointmentStatus.Cancelled}")
            .HasDatabaseName("UX_Appointments_Professional_Start_Active");
    }
}
