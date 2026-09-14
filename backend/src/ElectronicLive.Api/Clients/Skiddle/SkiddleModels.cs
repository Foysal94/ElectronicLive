using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

internal sealed record SkiddleResponse([property: JsonPropertyName("results")] List<SkiddleEvent>? Results);

internal sealed record SkiddleEvent(
    string? Id,
    [property: JsonPropertyName("eventname")] string? EventName,
    string? Date,
    [property: JsonPropertyName("openingtimes")] SkiddleOpeningTimes? OpeningTimes,
    string? Link,
    [property: JsonPropertyName("cancelled")] object? Cancelled,
    [property: JsonPropertyName("tickets")] object? Tickets,
    SkiddleVenue? Venue
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
            Id ?? string.Empty,
            EventName ?? string.Empty,
            venueName,
            date,
            time,
            Link,
            status,
            EventProvider.Skiddle
        );
    }

    private static EventStatus MapStatus(object? cancelled, object? tickets)
    {
        if (IsTruthy(cancelled))
        {
            return EventStatus.Cancelled;
        }

        if (IsFalsey(tickets))
        {
            return EventStatus.SoldOut;
        }

        if (IsTruthy(tickets))
        {
            return EventStatus.OnSale;
        }

        return EventStatus.Unknown;
    }

    private static bool IsTruthy(object? value) =>
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

    private static bool IsFalsey(object? value) =>
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
