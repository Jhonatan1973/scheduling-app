using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Infrastructure.Persistence.Configurations;

internal sealed class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public void Configure(EntityTypeBuilder<OutboxEmail> builder)
    {
        builder.ToTable("OutboxEmails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ToEmail).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ToName).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(300).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(1000);
        builder.HasIndex(e => new { e.Status, e.NextAttemptAtUtc });
    }
}
