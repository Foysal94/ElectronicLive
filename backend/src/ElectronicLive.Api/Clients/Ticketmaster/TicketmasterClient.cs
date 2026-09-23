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

    public EventProvider Provider => EventProvider.Ticketmaster;

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string? query,
        string? genre = null,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.Equals(_apiKey, "none", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Ticketmaster API key is not configured.");
            return [];
        }

        var genreKeyword = ResolveGenreKeyword(genre);
        var keyword = ResolveCombinedKeyword(query, genreKeyword);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        // Ticketmaster does not have specific genre filter fields, so we pass classificationName=music along with the genre keyword
        var requestUri =
            $"events.json?apikey={Uri.EscapeDataString(_apiKey)}"
            + $"&keyword={Uri.EscapeDataString(keyword)}"
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
                "Error occurred while querying Ticketmaster API for query '{Query}', genre '{Genre}'",
                query,
                genre
            );
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

    private static string? ResolveGenreKeyword(string? genre) =>
        genre?.Trim().ToLowerInvariant() switch
        {
            EventGenres.Techno => "Techno",
            EventGenres.House => "House",
            EventGenres.DrumAndBass => "Drum and Bass",
            EventGenres.Trance => "Trance",
            EventGenres.Garage => "UK Garage",
            _ => null,
        };

    private static string? ResolveCombinedKeyword(string? query, string? genreKeyword)
    {
        var trimmedQuery = query?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery) && !string.IsNullOrWhiteSpace(genreKeyword))
        {
            return $"{trimmedQuery} {genreKeyword}";
        }

        return !string.IsNullOrWhiteSpace(trimmedQuery) ? trimmedQuery : genreKeyword;
    }
}
