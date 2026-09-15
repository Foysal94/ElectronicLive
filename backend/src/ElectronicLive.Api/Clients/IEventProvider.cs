using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

public interface IEventProvider
{
    EventProvider Provider { get; }

    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string query,
        string city = "London",
        CancellationToken cancellationToken = default
    );
}
