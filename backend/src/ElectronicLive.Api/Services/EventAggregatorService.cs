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
                    return await provider.SearchEventsAsync(query, city, cancellationToken);
                }
                // Re-throw only if the caller cancelled. Upstream timeouts throw TaskCanceledException
                // (which inherits OperationCanceledException) while cancellationToken is untriggered,
                // and must be caught and isolated.
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Provider {Provider} failed while searching for query {Query}",
                        provider.Provider,
                        query
                    );
                    return (IReadOnlyList<EventResponse>)[];
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var allEvents = results.SelectMany(events => events ?? []);
        var deduplicated = deduplicator.Deduplicate(allEvents);

        return deduplicated
            // Push unannounced/TBA dates and times to the end of search results
            .OrderBy(e => e.Date ?? DateOnly.MaxValue)
            .ThenBy(e => e.Time ?? TimeOnly.MaxValue)
            .ToList();
    }
}
