using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

public interface IEventProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    );
}
