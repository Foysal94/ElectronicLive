namespace ElectronicLive.Api.Models;

/// <summary>
/// Represents an aggregated live music event in London or other tracked metropolitan areas.
/// </summary>
/// <param name="Id">Provider-specific unique event identifier.</param>
/// <param name="Name">Event or performance title.</param>
/// <param name="VenueName">Name of the hosting venue or club.</param>
/// <param name="Date">Calendar date of the event in ISO 8601 format (yyyy-MM-dd), or null if unannounced.</param>
/// <param name="Time">Door or start time in local venue time (HH:mm:ss), or null if unannounced.</param>
/// <param name="TicketUrl">Direct ticketing checkout or event listing URL.</param>
/// <param name="Status">Ticketing availability status.</param>
/// <param name="Provider">Primary ticketing or event discovery provider.</param>
/// <param name="Offers">Additional ticketing offers from other aggregated providers for deduplicated events.</param>
public sealed record EventResponse(
    string Id,
    string Name,
    string VenueName,
    DateOnly? Date,
    TimeOnly? Time,
    string? TicketUrl,
    EventStatus Status,
    EventProvider Provider,
    IReadOnlyList<EventTicketOffer>? Offers = null
);
