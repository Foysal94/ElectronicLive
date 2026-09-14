using System.Globalization;
using System.Text.Json;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Clients;

public sealed class SkiddleClient : ISkiddleClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<SkiddleClient> _logger;

    public SkiddleClient(HttpClient httpClient, IOptions<SkiddleOptions> options, ILogger<SkiddleClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = options.Value.ApiKey;
    }

    public EventProvider Provider => EventProvider.Skiddle;

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Skiddle API key is not configured.");
            return [];
        }

        var (lat, lon) = ResolveCoordinates(city);

        var requestUri =
            $"events/search/?api_key={Uri.EscapeDataString(_apiKey)}"
            + $"&keyword={Uri.EscapeDataString(artistName)}"
            + $"&latitude={lat.ToString(CultureInfo.InvariantCulture)}"
            + $"&longitude={lon.ToString(CultureInfo.InvariantCulture)}"
            + "&radius=25&order=date";

        SkiddleResponse? payload;
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Skiddle API returned non-success status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return [];
            }

            payload = await response.Content.ReadFromJsonAsync<SkiddleResponse>(JsonOptions, cancellationToken);
        }
        // Re-throw only if the caller cancelled. Upstream timeouts throw TaskCanceledException
        // (which inherits OperationCanceledException) while cancellationToken is untriggered,
        // and must be isolated rather than escaping.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while querying Skiddle API for artist {ArtistName}", artistName);
            return [];
        }

        var events = payload?.Results ?? [];
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

    private static (double Latitude, double Longitude) ResolveCoordinates(string city) =>
        city.ToLowerInvariant() switch
        {
            "manchester" => (53.4808, -2.2426),
            "birmingham" => (52.4862, -1.8904),
            "bristol" => (51.4545, -2.5879),
            "leeds" => (53.8008, -1.5491),
            _ => (51.5074, -0.1278), // Default London
        };
}
