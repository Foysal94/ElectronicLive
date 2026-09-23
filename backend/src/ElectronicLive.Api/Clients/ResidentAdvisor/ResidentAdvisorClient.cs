using System.Text.Json;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Clients;

public sealed class ResidentAdvisorClient : IResidentAdvisorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // RA's GraphQL EVENT index matches searchTerm against event names, line-up artists, and club/venue names
    private const string SearchQuery = """
        query SearchEvents($searchTerm: String!, $limit: Int!) {
          search(searchTerm: $searchTerm, indices: [EVENT], limit: $limit) {
            id
            value
            searchType
            contentUrl
            date
            clubName
            areaName
            countryName
          }
        }
        """;

    private readonly HttpClient _httpClient;
    private readonly ResidentAdvisorOptions _options;
    private readonly ILogger<ResidentAdvisorClient> _logger;

    public ResidentAdvisorClient(
        HttpClient httpClient,
        IOptions<ResidentAdvisorOptions> options,
        ILogger<ResidentAdvisorClient> logger
    )
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public EventProvider Provider => EventProvider.ResidentAdvisor;

    public async Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string? query,
        string? genre = null,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        var genreTerm = ResolveGenreSearchTerm(genre);
        var searchTerm = ResolveCombinedSearchTerm(query, genreTerm);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        // Resident Advisor has no dedicated genre filter in GraphQL, so we search for the genre in the main search term
        var requestPayload = new RaGraphQLRequest(SearchQuery, new { searchTerm = searchTerm, limit = _options.Limit });

        RaGraphQLResponse? payload;
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "graphql",
                requestPayload,
                JsonOptions,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Resident Advisor API returned non-success status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return [];
            }

            payload = await response.Content.ReadFromJsonAsync<RaGraphQLResponse>(JsonOptions, cancellationToken);
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
                "Error occurred while querying Resident Advisor API for query '{Query}', genre '{Genre}'",
                query,
                genre
            );
            return [];
        }

        if (payload?.HasErrors == true)
        {
            var errors = string.Join(", ", payload.Errors!.Select(e => e.Message ?? "Unknown error"));
            _logger.LogWarning("Resident Advisor GraphQL returned errors: {Errors}", errors);
            return [];
        }

        var items = payload?.Data?.Search ?? [];
        if (items.Count == 0)
        {
            return [];
        }

        var result = new List<EventResponse>(items.Count);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var item in items)
        {
            // Ignore past events from RA global search
            if (string.Equals(item.SearchType, "PASTEVENT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!MatchesCity(item.AreaName, item.CountryName, targetCity))
            {
                continue;
            }

            var ev = item.ToEventResponse();
            if (ev.Date.HasValue && ev.Date.Value < today)
            {
                continue;
            }

            result.Add(ev);
        }

        return result;
    }

    private static bool MatchesCity(string? areaName, string? countryName, string targetCity)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            return false;
        }

        if (!string.Equals(areaName.Trim(), targetCity, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Restrict to UK to prevent global namesake collisions (e.g. London, Canada or Newcastle, Australia)
        return string.IsNullOrWhiteSpace(countryName)
            || string.Equals(countryName.Trim(), "United Kingdom", StringComparison.OrdinalIgnoreCase)
            || string.Equals(countryName.Trim(), "UK", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveGenreSearchTerm(string? genre) =>
        genre?.Trim().ToLowerInvariant() switch
        {
            EventGenres.Techno => "Techno",
            EventGenres.House => "House",
            EventGenres.DrumAndBass => "Drum and Bass",
            EventGenres.Trance => "Trance",
            EventGenres.Garage => "UK Garage",
            _ => null,
        };

    private static string? ResolveCombinedSearchTerm(string? query, string? genreTerm)
    {
        var trimmedQuery = query?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery) && !string.IsNullOrWhiteSpace(genreTerm))
        {
            return $"{trimmedQuery} {genreTerm}";
        }

        return !string.IsNullOrWhiteSpace(trimmedQuery) ? trimmedQuery : genreTerm;
    }
}
