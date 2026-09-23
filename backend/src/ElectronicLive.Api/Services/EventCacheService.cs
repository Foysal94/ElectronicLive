using ElectronicLive.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public sealed class EventCacheService(HybridCache cache, ILogger<EventCacheService> logger) : IEventCacheService
{
    private static readonly string[] EventTags = ["events"];

    public async Task<IReadOnlyList<EventResponse>> GetOrAddAsync(
        string? query,
        string? genre,
        string city,
        Func<CancellationToken, Task<IReadOnlyList<EventResponse>>> factory,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(genre))
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var targetQuery = query?.Trim();
        var targetGenre = genre?.Trim().ToLowerInvariant();

        // Composite cache key isolates city, free-text query, and genre to prevent collisions between
        // overlapping artist queries and standalone genre filter hits.
        var cacheKey =
            $"events:agg:{targetCity.ToLowerInvariant()}:q={targetQuery?.ToLowerInvariant() ?? ""}:g={targetGenre ?? ""}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogInformation(
                    "Cache miss for query '{Query}', genre '{Genre}' in city '{City}'. Executing fetch factory.",
                    targetQuery,
                    targetGenre,
                    targetCity
                );
                return await factory(ct);
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );
    }
}
