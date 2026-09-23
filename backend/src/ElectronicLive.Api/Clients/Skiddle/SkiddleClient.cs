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
        var coordinates = SkiddleClientHelpers.ResolveCoordinates(targetCity);
        var requestUri = SkiddleClientHelpers.BuildSearchUri(_apiKey, query, genre, targetCity);

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

            if (!SkiddleClientHelpers.MatchesQuery(query, ev))
            {
                continue;
            }

            result.Add(ev.ToEventResponse());
        }

        return result;
    }

    internal static bool MatchesQuery(string? query, SkiddleEvent ev) => SkiddleClientHelpers.MatchesQuery(query, ev);
}
