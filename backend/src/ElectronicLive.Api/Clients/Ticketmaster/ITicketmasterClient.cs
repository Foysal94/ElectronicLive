using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

public interface ITicketmasterClient
{
    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    );
}
