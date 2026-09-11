using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface ITicketmasterService
{
    Task<IReadOnlyList<EventDto>> SearchEventsAsync(
        string artistName,
        string? city = "London",
        CancellationToken cancellationToken = default
    );
}
