namespace ElectronicLive.Api.Models;

public sealed record EventTicketOffer(EventProvider Provider, string? TicketUrl, EventStatus Status);
