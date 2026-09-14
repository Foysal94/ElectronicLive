using System.Globalization;
using System.Text.Json.Serialization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

internal sealed record RaGraphQLRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("variables")] object? Variables = null
);

internal sealed record RaGraphQLResponse(
    [property: JsonPropertyName("data")] RaData? Data,
    [property: JsonPropertyName("errors")] IReadOnlyList<RaGraphQLError>? Errors
)
{
    public bool HasErrors => Errors is { Count: > 0 };
}

internal sealed record RaGraphQLError([property: JsonPropertyName("message")] string? Message);

internal sealed record RaData([property: JsonPropertyName("search")] List<RaSearchItem>? Search);

internal sealed record RaSearchItem(
    string? Id,
    string? Value,
    string? SearchType,
    string? ContentUrl,
    string? Date,
    string? ClubName,
    string? AreaName,
    string? CountryName
)
{
    public EventResponse ToEventResponse()
    {
        var rawVenue = ClubName;
        var venueName = string.IsNullOrWhiteSpace(rawVenue) ? "Unknown Venue" : rawVenue.Trim();
        var status = ResolveStatus(Value);

        DateOnly? date = null;
        TimeOnly? time = null;

        if (
            !string.IsNullOrWhiteSpace(Date)
            && DateTime.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
        )
        {
            date = DateOnly.FromDateTime(dt);
            if (Date.Contains('T') || Date.Contains(':'))
            {
                time = TimeOnly.FromDateTime(dt);
            }
        }

        var ticketUrl = FormatTicketUrl(ContentUrl);

        return new EventResponse(
            Id ?? string.Empty,
            Value ?? string.Empty,
            venueName,
            date,
            time,
            ticketUrl,
            status,
            EventProvider.ResidentAdvisor
        );
    }

    private static string? FormatTicketUrl(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            return null;
        }

        if (
            contentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || contentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            return contentUrl;
        }

        return contentUrl.StartsWith('/') ? $"https://ra.co{contentUrl}" : $"https://ra.co/{contentUrl}";
    }

    private static EventStatus ResolveStatus(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return EventStatus.OnSale;
        }

        if (title.Contains("sold out", StringComparison.OrdinalIgnoreCase))
        {
            return EventStatus.SoldOut;
        }

        if (
            title.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
            || title.Contains("canceled", StringComparison.OrdinalIgnoreCase)
        )
        {
            return EventStatus.Cancelled;
        }

        if (title.Contains("postponed", StringComparison.OrdinalIgnoreCase))
        {
            return EventStatus.Postponed;
        }

        return EventStatus.OnSale;
    }
}
