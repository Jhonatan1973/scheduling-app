using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SchedulingApp.Application.Abstractions;
using SchedulingApp.Application.Dtos;

namespace SchedulingApp.Infrastructure.Caching;

public class RedisSlotCache(IDistributedCache cache) : ISlotCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TimeSlotDto>?> GetAsync(
        Guid professionalId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(BuildKey(professionalId, date), cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<TimeSlotDto>>(bytes, JsonOptions);
    }

    public async Task SetAsync(
        Guid professionalId,
        DateOnly date,
        IReadOnlyList<TimeSlotDto> slots,
        CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(slots, JsonOptions);
        await cache.SetAsync(
            BuildKey(professionalId, date),
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);
    }

    public Task InvalidateAsync(Guid professionalId, DateOnly date, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(BuildKey(professionalId, date), cancellationToken);

    private static string BuildKey(Guid professionalId, DateOnly date) =>
        $"slots:{professionalId:N}:{date:yyyy-MM-dd}";
}
