using System.Text.Json;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Clients;

public sealed class TicketmasterClient : ITicketmasterClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<TicketmasterClient> _logger;

    public TicketmasterClient(
        HttpClient httpClient,
        IOptions<TicketmasterOptions> options,
        ILogger<TicketmasterClient> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Ticketmaster API key is not configured.");
            return [];
        }

        var requestUri =
            $"events.json?apikey={Uri.EscapeDataString(_apiKey)}"
            + $"&keyword={Uri.EscapeDataString(artistName)}"
            + $"&city={Uri.EscapeDataString(city)}"
            + "&countryCode=GB&classificationName=music&sort=date,asc";

        TicketmasterResponse? payload;
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ticketmaster API returned non-success status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return [];
            }

            payload = await response.Content.ReadFromJsonAsync<TicketmasterResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while querying Ticketmaster API for artist {ArtistName}", artistName);
            return [];
        }

        var events = payload?.Embedded?.Events ?? [];
        if (events.Count == 0)
        {
            return [];
        }

        var result = new List<EventResponse>(events.Count);
        foreach (var ev in events)
        {
            result.Add(ev.ToEventResponse());
        }

        return result;
    }
}
