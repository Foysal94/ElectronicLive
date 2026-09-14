namespace ElectronicLive.Api.Models;

public sealed record EventResponse(
    string Id,
    string Name,
    string VenueName,
    DateOnly? Date,
    TimeOnly? Time,
    string? TicketUrl,
    EventStatus Status,
    string Provider
);
