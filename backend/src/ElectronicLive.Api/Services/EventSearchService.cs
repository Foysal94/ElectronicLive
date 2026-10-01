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
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default
    )
    {
        var hasQueryOrGenre = !string.IsNullOrWhiteSpace(query) || !string.IsNullOrWhiteSpace(genre);
        var hasDateFilter = from.HasValue || to.HasValue;

        if (!hasQueryOrGenre && !hasDateFilter)
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var targetQuery = query?.Trim();
        var targetGenre = genre?.Trim().ToLowerInvariant();

        // For artist/genre queries, cache canonical schedules and slice dates in memory.
        // For broad date-only queries, isolate cache by date range and forward bounds upstream.
        var cacheKey = hasQueryOrGenre
            ? $"events:agg:{targetCity.ToLowerInvariant()}:q={targetQuery?.ToLowerInvariant() ?? ""}:g={targetGenre ?? ""}"
            : $"events:agg:{targetCity.ToLowerInvariant()}:date:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";

        var cachedEvents = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                logger.LogInformation(
                    "Cache miss for query '{Query}', genre '{Genre}', from '{From}', to '{To}' in city '{City}'. Querying upstream providers.",
                    targetQuery,
                    targetGenre,
                    from,
                    to,
                    targetCity
                );

                var upstreamFrom = hasQueryOrGenre ? null : from;
                var upstreamTo = hasQueryOrGenre ? null : to;

                var rawEvents = await FetchFromProvidersAsync(
                    targetQuery,
                    targetGenre,
                    targetCity,
                    upstreamFrom,
                    upstreamTo,
                    ct
                );
                var deduplicated = EventDeduplicator.Deduplicate(rawEvents);

                return deduplicated
                    .OrderBy(e => e.Date ?? DateOnly.MaxValue)
                    .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
                    .ToList();
            },
            tags: EventTags,
            cancellationToken: cancellationToken
        );

        if (hasDateFilter)
        {
            return cachedEvents
                .Where(e =>
                    e.Date.HasValue
                    && (!from.HasValue || e.Date.Value >= from.Value)
                    && (!to.HasValue || e.Date.Value <= to.Value)
                )
                .ToList();
        }

        return cachedEvents;
    }

    private async Task<IReadOnlyList<EventResponse>> FetchFromProvidersAsync(
        string? query,
        string? genre,
        string city,
        DateOnly? from,
        DateOnly? to,
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
                    var events = await provider.SearchEventsAsync(query, genre, city, from, to, cancellationToken);
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

        return results.Where(r => r.Success).SelectMany(r => r.Events ?? []).ToList();
    }
}
