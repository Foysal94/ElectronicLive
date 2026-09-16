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

        var normalizedCity = string.IsNullOrWhiteSpace(city) ? "london" : city.Trim().ToLowerInvariant();
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var cacheKey = $"events:agg:{normalizedCity}:{normalizedQuery}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Cache miss for query '{Query}' in city '{City}'. Fetching from upstream providers.",
                        query,
                        city
                    );
                }
                return await inner.SearchEventsAsync(query, city, ct);
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );
    }
}
