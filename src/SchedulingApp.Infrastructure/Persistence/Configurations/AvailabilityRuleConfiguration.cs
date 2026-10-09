using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRules");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.ProfessionalId, r.DayOfWeek });
    }
}
