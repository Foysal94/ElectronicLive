using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Services;

public sealed partial class TicketmasterService : ITicketmasterService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<TicketmasterService> _logger;

    public TicketmasterService(
        HttpClient httpClient,
        IOptions<TicketmasterOptions> options,
        ILogger<TicketmasterService> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogApiKeyMissing(_logger);
            return [];
        }

        var effectiveCity = string.IsNullOrWhiteSpace(city) ? "London" : city;
        var requestUri =
            $"events.json?apikey={Uri.EscapeDataString(_apiKey)}"
            + $"&keyword={Uri.EscapeDataString(artistName)}"
            + $"&city={Uri.EscapeDataString(effectiveCity)}"
            + "&countryCode=GB&classificationName=music&sort=date,asc";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogNonSuccessStatus(_logger, response.StatusCode, response.ReasonPhrase);
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<TicketmasterResponse>(
                JsonOptions,
                cancellationToken
            );

            var events = payload?.Embedded?.Events;
            if (events is null || events.Count == 0)
            {
                return [];
            }

            var result = new List<EventResponse>(events.Count);
            foreach (var ev in events)
            {
                var venueName = ev.Embedded?.Venues?.FirstOrDefault()?.Name ?? "Unknown Venue";
                var status = MapStatus(ev.Dates?.Status?.Code);

                result.Add(
                    new EventResponse(
                        ev.Id ?? string.Empty,
                        ev.Name ?? string.Empty,
                        venueName,
                        ev.Dates?.Start?.LocalDate,
                        ev.Dates?.Start?.LocalTime,
                        ev.Url,
                        status
                    )
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            LogApiError(_logger, ex, artistName);
            return [];
        }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ticketmaster API key is not configured.")]
    private static partial void LogApiKeyMissing(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ticketmaster API returned non-success status code {StatusCode}: {ReasonPhrase}"
    )]
    private static partial void LogNonSuccessStatus(ILogger logger, HttpStatusCode statusCode, string? reasonPhrase);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error occurred while querying Ticketmaster API for artist {ArtistName}"
    )]
    private static partial void LogApiError(ILogger logger, Exception ex, string artistName);

    internal sealed record TicketmasterResponse(
        [property: JsonPropertyName("_embedded")] TicketmasterEmbedded? Embedded
    );

    internal sealed record TicketmasterEmbedded(List<TicketmasterEvent>? Events);

    internal sealed record TicketmasterEvent(
        string? Id,
        string? Name,
        string? Url,
        TicketmasterDates? Dates,
        [property: JsonPropertyName("_embedded")] TicketmasterEventEmbedded? Embedded
    );

    internal sealed record TicketmasterEventEmbedded(List<TicketmasterVenue>? Venues);

    internal sealed record TicketmasterVenue(string? Name);

    internal sealed record TicketmasterDates(TicketmasterStart? Start, TicketmasterStatus? Status);

    internal sealed record TicketmasterStart(string? LocalDate, string? LocalTime);

    internal sealed record TicketmasterStatus(string? Code);
}
