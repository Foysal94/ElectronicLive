using ElectronicLive.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public sealed class EventCacheService(HybridCache cache, ILogger<EventCacheService> logger) : IEventCacheService
{
    private static readonly string[] EventTags = ["events"];

    public async Task<IReadOnlyList<EventResponse>> GetOrAddAsync(
        string query,
        string city,
        Func<CancellationToken, Task<IReadOnlyList<EventResponse>>> factory,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var targetQuery = query.Trim();
        var cacheKey = $"events:agg:{targetCity.ToLowerInvariant()}:{targetQuery.ToLowerInvariant()}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogInformation(
                    "Cache miss for query '{Query}' in city '{City}'. Executing fetch factory.",
                    targetQuery,
                    targetCity
                );
                return await factory(ct);
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );
    }
}
