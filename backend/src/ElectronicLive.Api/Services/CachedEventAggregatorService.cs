using ElectronicLive.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public sealed class CachedEventAggregatorService(
    IEventAggregatorService inner,
    HybridCache cache,
    ILogger<CachedEventAggregatorService> logger
) : IEventAggregatorService
{
    private static readonly string[] EventTags = ["events"];

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string query,
        string city = "London",
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
                    "Cache miss for query '{Query}' in city '{City}'. Fetching from upstream providers.",
                    targetQuery,
                    targetCity
                );
                return await inner.SearchEventsAsync(targetQuery, targetCity, ct);
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );
    }
}
