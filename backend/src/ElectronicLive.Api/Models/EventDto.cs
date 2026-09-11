namespace ElectronicLive.Api.Models;

public sealed record EventDto(
    string Id,
    string Name,
    string VenueName,
    string? Date,
    string? Time,
    string? TicketUrl,
    EventStatus Status
);
