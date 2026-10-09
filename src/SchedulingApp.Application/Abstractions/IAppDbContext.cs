using Microsoft.EntityFrameworkCore;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Application.Abstractions;

/// <summary>Persistence port used by the use cases. Implemented by the EF Core context in Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<Professional> Professionals { get; }
    DbSet<AvailabilityRule> AvailabilityRules { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<OutboxEmail> OutboxEmails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
