using SchedulingApp.Application.Dtos;

namespace SchedulingApp.Application.Abstractions;

public interface ISlotCache
{
    Task<IReadOnlyList<TimeSlotDto>?> GetAsync(Guid professionalId, DateOnly date, CancellationToken cancellationToken = default);
    Task SetAsync(Guid professionalId, DateOnly date, IReadOnlyList<TimeSlotDto> slots, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid professionalId, DateOnly date, CancellationToken cancellationToken = default);
}
