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
        var deduplicated = Deduplicate(allEvents);

        return deduplicated.OrderBy(e => e.Date ?? DateOnly.MaxValue).ThenBy(e => e.Time ?? TimeOnly.MaxValue).ToList();
    }

    private static List<EventResponse> Deduplicate(IEnumerable<EventResponse> events)
    {
        var deduplicated = new List<EventResponse>();
        var datedGroups = new Dictionary<(DateOnly Date, string Venue), List<EventResponse>>();

        foreach (var ev in events)
        {
            // Events without a confirmed date cannot be safely correlated across providers
            // without risking false-positive merges for different tour stops.
            if (!ev.Date.HasValue)
            {
                deduplicated.Add(EnsureOffers(ev));
                continue;
            }

            var key = (Date: ev.Date.Value, Venue: NormalizeVenue(ev.VenueName));
            if (!datedGroups.TryGetValue(key, out var group))
            {
                group = [];
                datedGroups[key] = group;
            }

            group.Add(ev);
        }

        foreach (var group in datedGroups.Values)
        {
            if (group.Count == 1)
            {
                deduplicated.Add(EnsureOffers(group[0]));
                continue;
            }

            deduplicated.Add(MergeEvents(group));
        }

        return deduplicated;
    }

    private static EventResponse MergeEvents(List<EventResponse> duplicates)
    {
        // Prioritize the offer with the highest availability status (e.g. OnSale over SoldOut)
        // so the primary ticket link provides immediate buying capability to the caller.
        var primary = duplicates
            .OrderByDescending(e => StatusPriority(e.Status))
            .ThenBy(e => ProviderPriority(e.Provider))
            .First();

        // Prefer earlier start/door time if one provider specifies doors and another show start
        var times = duplicates.Where(e => e.Time.HasValue).Select(e => e.Time!.Value).ToList();
        TimeOnly? earliestTime = times.Count > 0 ? times.Min() : null;

        var offers = duplicates
            .SelectMany(e => e.Offers ?? [new EventTicketOffer(e.Provider, e.TicketUrl, e.Status)])
            .DistinctBy(o => o.Provider)
            .ToList();

        return new EventResponse(
            primary.Id,
            primary.Name,
            primary.VenueName,
            primary.Date,
            earliestTime,
            primary.TicketUrl,
            primary.Status,
            primary.Provider,
            offers
        );
    }

    private static EventResponse EnsureOffers(EventResponse ev) =>
        ev.Offers is not null ? ev : ev with { Offers = [new EventTicketOffer(ev.Provider, ev.TicketUrl, ev.Status)] };

    // Prioritize confirmed statuses over Unknown so unverified provider data does not overwrite verified SoldOut or Cancelled states
    private static int StatusPriority(EventStatus status) =>
        status switch
        {
            EventStatus.OnSale => 5,
            EventStatus.SoldOut => 4,
            EventStatus.Postponed => 3,
            EventStatus.Cancelled => 2,
            EventStatus.Unknown => 1,
            _ => 0,
        };

    private static int ProviderPriority(EventProvider provider) =>
        provider switch
        {
            EventProvider.Ticketmaster => 0,
            EventProvider.Skiddle => 1,
            EventProvider.ResidentAdvisor => 2,
            _ => 99,
        };

    // Normalizes venue names by stripping leading articles ("The "), common city suffixes,
    // and non-alphanumeric punctuation to match cross-vendor differences (e.g., "The Drumsheds" vs "Drumsheds, London").
    private static string NormalizeVenue(string venue)
    {
        if (string.IsNullOrWhiteSpace(venue))
        {
            return string.Empty;
        }

        var v = venue.Trim().ToLowerInvariant();
        if (v.StartsWith("the ", StringComparison.Ordinal))
        {
            v = v[4..].Trim();
        }

        var suffixes = new[] { ", london", " london", ", uk", ", england", ", manchester", " manchester" };
        var matchedSuffix = suffixes.FirstOrDefault(s => v.EndsWith(s, StringComparison.Ordinal));
        if (matchedSuffix != null)
        {
            v = v[..^matchedSuffix.Length].Trim();
        }

        return string.Concat(v.Where(char.IsLetterOrDigit));
    }
}
