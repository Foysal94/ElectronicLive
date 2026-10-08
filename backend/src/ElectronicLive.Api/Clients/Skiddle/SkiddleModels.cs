using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

internal sealed record SkiddleResponse(
    [property: JsonPropertyName("error")] object? Error,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("results")] List<SkiddleEvent>? Results
)
{
    public bool HasError => SkiddleTypeCoercion.IsTruthy(Error);
}

internal sealed record SkiddleEvent(
    string? Id,
    [property: JsonPropertyName("eventname")] string? EventName,
    string? Date,
    [property: JsonPropertyName("openingtimes")] SkiddleOpeningTimes? OpeningTimes,
    string? Link,
    [property: JsonPropertyName("cancelled")] object? Cancelled,
    [property: JsonPropertyName("tickets")] object? Tickets,
    SkiddleVenue? Venue,
    [property: JsonPropertyName("artists")] List<SkiddleArtist>? Artists = null
)
{
    public EventResponse ToEventResponse()
    {
        var rawVenue = Venue?.Name;
        var venueName = string.IsNullOrWhiteSpace(rawVenue) ? "Unknown Venue" : rawVenue;
        var status = MapStatus(Cancelled, Tickets);

        DateOnly? date = DateOnly.TryParse(Date, System.Globalization.CultureInfo.InvariantCulture, out var parsedDate)
            ? parsedDate
            : null;

        TimeOnly? time = TimeOnly.TryParse(
            OpeningTimes?.DoorsOpen,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedTime
        )
            ? parsedTime
            : null;

        return new EventResponse(
            Id: Id ?? string.Empty,
            Name: EventName ?? string.Empty,
            VenueName: venueName,
            Date: date,
            Time: time,
            TicketUrl: Link,
            Status: status,
            Provider: EventProvider.Skiddle
        );
    }

    private static EventStatus MapStatus(object? cancelled, object? tickets)
    {
        if (SkiddleTypeCoercion.IsTruthy(cancelled))
        {
            return EventStatus.Cancelled;
        }

        if (SkiddleTypeCoercion.IsFalsey(tickets))
        {
            return EventStatus.SoldOut;
        }

        if (SkiddleTypeCoercion.IsTruthy(tickets))
        {
            return EventStatus.OnSale;
        }

        return EventStatus.Unknown;
    }
}

file static class SkiddleTypeCoercion
{
    public static bool IsTruthy(object? value) =>
        value switch
        {
            bool b => b,
            string s => s is "1" or "true" or "True",
            JsonElement elem when elem.ValueKind == JsonValueKind.True => true,
            JsonElement elem when elem.ValueKind == JsonValueKind.Number => elem.GetInt32() == 1,
            JsonElement elem when elem.ValueKind == JsonValueKind.String => elem.GetString() is "1" or "true" or "True",
            int i => i == 1,
            _ => false,
        };

    public static bool IsFalsey(object? value) =>
        value switch
        {
            bool b => !b,
            string s => s is "0" or "false" or "False",
            JsonElement elem when elem.ValueKind == JsonValueKind.False => true,
            JsonElement elem when elem.ValueKind == JsonValueKind.Number => elem.GetInt32() == 0,
            JsonElement elem when elem.ValueKind == JsonValueKind.String => elem.GetString()
                is "0"
                    or "false"
                    or "False",
            int i => i == 0,
            _ => false,
        };
}

internal sealed record SkiddleVenue(string? Name, string? Town);

internal sealed record SkiddleOpeningTimes([property: JsonPropertyName("doorsopen")] string? DoorsOpen);

internal sealed record SkiddleArtist(
    [property: JsonPropertyName("artistid")] string? ArtistId,
    [property: JsonPropertyName("name")] string? Name
);
