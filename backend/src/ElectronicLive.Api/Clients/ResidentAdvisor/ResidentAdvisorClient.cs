using System.Text.Json;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Clients;

public sealed class ResidentAdvisorClient : IResidentAdvisorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
        string artistName,
        string city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return [];
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var requestPayload = new RaGraphQLRequest(SearchQuery, new { searchTerm = artistName, limit = _options.Limit });

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
                "Error occurred while querying Resident Advisor API for artist {ArtistName}",
                artistName
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
        foreach (var item in items)
        {
            if (!MatchesCity(item.AreaName, targetCity))
            {
                continue;
            }

            result.Add(item.ToEventResponse());
        }

        return result;
    }

    private static bool MatchesCity(string? areaName, string targetCity)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            return false;
        }

        return string.Equals(areaName, targetCity, StringComparison.OrdinalIgnoreCase)
            || areaName.Contains(targetCity, StringComparison.OrdinalIgnoreCase);
    }
}
