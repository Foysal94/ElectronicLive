using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface ITicketmasterService
{
    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string? city = "London",
        CancellationToken cancellationToken = default
    );
}
