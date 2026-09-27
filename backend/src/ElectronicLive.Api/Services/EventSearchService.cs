using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public sealed class EventSearchService(
    IEnumerable<IEventProvider> providers,
    HybridCache cache,
    ILogger<EventSearchService> logger
) : IEventSearchService
{
    private static readonly string[] EventTags = ["events"];

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string? query,
        string? genre = null,
        string city = "London",
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
                    "Cache miss for query '{Query}', genre '{Genre}' in city '{City}'. Querying upstream providers.",
                    targetQuery,
                    targetGenre,
                    targetCity
                );
                return await FetchAndDeduplicateAsync(targetQuery, targetGenre, targetCity, ct);
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );
    }

    private async Task<IReadOnlyList<EventResponse>> FetchAndDeduplicateAsync(
        string? query,
        string? genre,
        string city,
        CancellationToken cancellationToken
    )
    {
        var providerList = providers as IReadOnlyCollection<IEventProvider> ?? providers.ToList();
        if (providerList.Count == 0)
        {
            logger.LogWarning("No event providers are configured.");
            return [];
        }

        var tasks = providerList
            .Select(async provider =>
            {
                try
                {
                    var events = await provider.SearchEventsAsync(query, genre, city, cancellationToken);
                    return (Success: true, Events: events, Provider: provider.Provider, Exception: (Exception?)null);
                }
                // Re-throw only if the caller cancelled. Upstream timeouts throw TaskCanceledException
                // (which inherits OperationCanceledException) while cancellationToken is untriggered,
                // and must be isolated.
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return (
                        Success: false,
                        Events: (IReadOnlyList<EventResponse>)[],
                        Provider: provider.Provider,
                        Exception: ex
                    );
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var failedCount = results.Count(r => !r.Success);
        if (failedCount == providerList.Count)
        {
            logger.LogError(
                "All {ProviderCount} event providers failed while searching for query '{Query}', genre '{Genre}'",
                providerList.Count,
                query,
                genre
            );
            throw new AllProvidersUnavailableException(query, genre, providerList.Count);
        }

        foreach (var failed in results.Where(r => !r.Success))
        {
            logger.LogWarning(
                failed.Exception,
                "Provider {Provider} failed while searching for query '{Query}', genre '{Genre}'",
                failed.Provider,
                query,
                genre
            );
        }

        var allEvents = results.Where(r => r.Success).SelectMany(r => r.Events ?? []);
        var deduplicated = EventDeduplicator.Deduplicate(allEvents);

        return deduplicated.OrderBy(e => e.Date ?? DateOnly.MaxValue).ThenBy(e => e.Time ?? TimeOnly.MaxValue).ToList();
    }
}
