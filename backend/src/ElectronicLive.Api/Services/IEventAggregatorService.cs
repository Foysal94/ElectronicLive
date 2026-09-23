using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEventAggregatorService
{
    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string? query,
        string? genre = null,
        string city = "London",
        CancellationToken cancellationToken = default
    );
}
