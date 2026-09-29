using System.Text.Json.Serialization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

internal sealed record TicketmasterResponse([property: JsonPropertyName("_embedded")] TicketmasterEmbedded? Embedded);

internal sealed record TicketmasterEmbedded(List<TicketmasterEvent>? Events);

internal sealed record TicketmasterEvent(
    string? Id,
    string? Name,
    string? Url,
    TicketmasterDates? Dates,
    [property: JsonPropertyName("_embedded")] TicketmasterEventEmbedded? Embedded
)
{
    public EventResponse ToEventResponse()
    {
        var rawVenue = Embedded?.Venues?.FirstOrDefault()?.Name;
        var venueName = string.IsNullOrWhiteSpace(rawVenue) ? "Unknown Venue" : rawVenue;
        var status = MapStatus(Dates?.Status?.Code);
        DateOnly? date = DateOnly.TryParse(
            Dates?.Start?.LocalDate,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedDate
        )
            ? parsedDate
            : null;
        TimeOnly? time = TimeOnly.TryParse(
            Dates?.Start?.LocalTime,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedTime
        )
            ? parsedTime
            : null;

        return new EventResponse(
            Id ?? string.Empty,
            Name ?? string.Empty,
            venueName,
            date,
            time,
            Url,
            status,
            EventProvider.Ticketmaster
        );
    }

    private static EventStatus MapStatus(string? code) =>
        code?.ToLowerInvariant() switch
        {
            "onsale" => EventStatus.OnSale,
            "offsale" => EventStatus.SoldOut,
            "canceled" or "cancelled" => EventStatus.Cancelled,
            "postponed" or "rescheduled" => EventStatus.Postponed,
            _ => EventStatus.Unknown,
        };
}

internal sealed record TicketmasterEventEmbedded(List<TicketmasterVenue>? Venues);

internal sealed record TicketmasterVenue(string? Name);

internal sealed record TicketmasterDates(TicketmasterStart? Start, TicketmasterStatus? Status);

internal sealed record TicketmasterStart(string? LocalDate, string? LocalTime);

internal sealed record TicketmasterStatus(string? Code);

internal sealed record TicketmasterAttractionsResponse(
    [property: JsonPropertyName("_embedded")] TicketmasterAttractionsEmbedded? Embedded,
    [property: JsonPropertyName("page")] TicketmasterAttractionsPage? Page
);

internal sealed record TicketmasterAttractionsEmbedded(
    [property: JsonPropertyName("attractions")] List<TicketmasterAttractionItem>? Attractions
);

internal sealed record TicketmasterAttractionItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name
);

internal sealed record TicketmasterAttractionsPage([property: JsonPropertyName("totalElements")] int TotalElements);
