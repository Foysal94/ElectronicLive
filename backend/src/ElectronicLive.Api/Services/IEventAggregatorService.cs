using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEventAggregatorService
{
    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    );
}
