namespace ElectronicLive.Api.Models;

/// <summary>
/// Represents a ticketing offer from a specific provider for a multi-listed event.
/// </summary>
/// <param name="Provider">Ticketing provider for this offer.</param>
/// <param name="TicketUrl">Direct ticketing checkout or event listing URL.</param>
/// <param name="Status">Ticketing availability status for this provider allocation.</param>
public sealed record EventTicketOffer(EventProvider Provider, string? TicketUrl, EventStatus Status);
