using System.Text.Json;
using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Services;

public sealed class TicketmasterArtistVerificationService : IArtistVerificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<TicketmasterArtistVerificationService> _logger;

    public TicketmasterArtistVerificationService(
        HttpClient httpClient,
        IOptions<TicketmasterOptions> options,
        ILogger<TicketmasterArtistVerificationService> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = options.Value.ApiKey;
    }

    public async Task<bool> VerifyArtistExistsAsync(string artistName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_apiKey) || string.Equals(_apiKey, "none", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Ticketmaster API key is not configured for artist verification.");
            return false;
        }

        var trimmedArtist = artistName.Trim();
        var requestUri =
            $"attractions.json?apikey={Uri.EscapeDataString(_apiKey)}"
            + $"&keyword={Uri.EscapeDataString(trimmedArtist)}"
            + "&classificationName=music";

        TicketmasterAttractionsResponse? payload;
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ticketmaster Attractions API returned non-success status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return false;
            }

            payload = await response.Content.ReadFromJsonAsync<TicketmasterAttractionsResponse>(
                JsonOptions,
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while querying Ticketmaster Attractions API for artist '{ArtistName}'",
                artistName
            );
            return false;
        }

        var attractions = payload?.Embedded?.Attractions;
        if (attractions == null || attractions.Count == 0)
        {
            return false;
        }

        return attractions.Any(a =>
            !string.IsNullOrWhiteSpace(a.Name)
            && (
                string.Equals(a.Name, trimmedArtist, StringComparison.OrdinalIgnoreCase)
                || a.Name.Contains(trimmedArtist, StringComparison.OrdinalIgnoreCase)
            )
        );
    }
}
