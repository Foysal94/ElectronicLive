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
        string? query,
        string? genre = null,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.Equals(_apiKey, "none", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Skiddle API key is not configured.");
            return [];
        }

        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(genre))
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var coordinates = ResolveCoordinates(targetCity);
        var geoQuery = coordinates.HasValue
            ? $"&latitude={coordinates.Value.Latitude.ToString(CultureInfo.InvariantCulture)}&longitude={coordinates.Value.Longitude.ToString(CultureInfo.InvariantCulture)}&radius=25"
            : string.Empty;

        // If query is empty, omit &keyword= so Skiddle does not return empty results for genre filtering
        var keywordParam = !string.IsNullOrWhiteSpace(query)
            ? $"&keyword={Uri.EscapeDataString(query.Trim())}"
            : string.Empty;

        var genreId = ResolveGenreId(genre);
        var genreParam = genreId.HasValue ? $"&g={genreId.Value}&eventcode=CLUB" : "&eventcode=LIVE,CLUB,FEST";

        var requestUri =
            $"events/search/?api_key={Uri.EscapeDataString(_apiKey)}"
            + keywordParam
            + geoQuery
            + genreParam
            + "&order=date&description=1";

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
            _logger.LogError(
                ex,
                "Error occurred while querying Skiddle API for query '{Query}', genre '{Genre}'",
                query,
                genre
            );
            return [];
        }

        if (payload?.HasError == true)
        {
            _logger.LogWarning(
                "Skiddle API returned error response: {Description}",
                payload.Description ?? "Unknown upstream error"
            );
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
            if (!coordinates.HasValue && !string.Equals(ev.Venue?.Town, targetCity, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!MatchesQuery(query, ev))
            {
                continue;
            }

            result.Add(ev.ToEventResponse());
        }

        return result;
    }

    private static int? ResolveGenreId(string? genre) =>
        genre?.Trim().ToLowerInvariant() switch
        {
            EventGenres.Techno => 4,
            EventGenres.House => 1,
            EventGenres.DrumAndBass => 7,
            EventGenres.Trance => 5,
            EventGenres.Garage => 26,
            _ => null,
        };

    internal static bool MatchesQuery(string? query, SkiddleEvent ev)
    {
        // When searching by genre with no free-text query, preserve all returned club events.
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var trimmedQuery = query.Trim();

        if (ev.EventName?.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (ev.Venue?.Name?.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (
            ev.Artists?.Any(a =>
                !string.IsNullOrWhiteSpace(a.Name) && a.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)
            ) == true
        )
        {
            return true;
        }

        var tokens = trimmedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length > 1)
        {
            var artistsText =
                ev.Artists != null
                    ? string.Join(" ", ev.Artists.Where(a => !string.IsNullOrWhiteSpace(a.Name)).Select(a => a.Name))
                    : string.Empty;

            var searchable = $"{ev.EventName} {ev.Venue?.Name} {artistsText}";

            if (tokens.All(token => searchable.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static (double Latitude, double Longitude)? ResolveCoordinates(string city) =>
        city.ToLowerInvariant() switch
        {
            "london" => (51.5074, -0.1278),
            "manchester" => (53.4808, -2.2426),
            "birmingham" => (52.4862, -1.8904),
            "bristol" => (51.4545, -2.5879),
            "leeds" => (53.8008, -1.5491),
            "glasgow" => (55.8642, -4.2518),
            "liverpool" => (53.4084, -2.9916),
            "brighton" => (50.8225, -0.1372),
            "sheffield" => (53.3811, -1.4701),
            "newcastle" => (54.9783, -1.6178),
            _ => null,
        };
}
