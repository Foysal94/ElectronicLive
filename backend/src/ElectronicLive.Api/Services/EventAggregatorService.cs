using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public sealed class EventAggregatorService(
    IEnumerable<IEventProvider> providers,
    ILogger<EventAggregatorService> logger
) : IEventAggregatorService
{
    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
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

        var tasks = providerList.Select(async provider =>
        {
            try
            {
                return await provider.SearchEventsAsync(artistName, city, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Provider {ProviderName} failed while searching for artist {ArtistName}",
                    provider.ProviderName,
                    artistName
                );
                return (IReadOnlyList<EventResponse>)[];
            }
        });

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(events => events).OrderBy(e => e.Date).ThenBy(e => e.Time).ToList();
    }
}
