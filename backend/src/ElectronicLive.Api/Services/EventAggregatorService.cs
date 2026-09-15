using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public sealed class EventAggregatorService(
    IEnumerable<IEventProvider> providers,
    IEventDeduplicator deduplicator,
    ILogger<EventAggregatorService> logger
) : IEventAggregatorService
{
    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string query,
        string city = "London",
        CancellationToken cancellationToken = default
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
                    var events = await provider.SearchEventsAsync(query, city, cancellationToken);
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
                        Exception: (Exception?)ex
                    );
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var failedCount = results.Count(r => !r.Success);
        if (failedCount == providerList.Count)
        {
            logger.LogError(
                "All {ProviderCount} event providers failed while searching for query {Query}",
                providerList.Count,
                query
            );
            throw new AllProvidersUnavailableException(query, providerList.Count);
        }

        foreach (var failed in results.Where(r => !r.Success))
        {
            logger.LogWarning(
                failed.Exception,
                "Provider {Provider} failed while searching for query {Query}",
                failed.Provider,
                query
            );
        }

        var allEvents = results.Where(r => r.Success).SelectMany(r => r.Events ?? []);
        var deduplicated = deduplicator.Deduplicate(allEvents);

        return deduplicated
            // Push unannounced/TBA dates and times to the end of search results
            .OrderBy(e => e.Date ?? DateOnly.MaxValue)
            .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();
    }
}
