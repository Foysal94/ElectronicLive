using System.Globalization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Clients;

internal static class SkiddleClientHelpers
{
    // Skiddle API requires coordinates for distance searches; without them, it defaults to UK-wide events.
    internal static string BuildSearchUri(string apiKey, string? query, string? genre, string city)
    {
        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var coordinates = ResolveCoordinates(targetCity);
        var geoQuery = coordinates.HasValue
            ? $"&latitude={coordinates.Value.Latitude.ToString(CultureInfo.InvariantCulture)}&longitude={coordinates.Value.Longitude.ToString(CultureInfo.InvariantCulture)}&radius=25"
            : string.Empty;

        // Skiddle returns zero results if an empty keyword parameter is passed alongside genre IDs.
        var keywordParam = !string.IsNullOrWhiteSpace(query)
            ? $"&keyword={Uri.EscapeDataString(query.Trim())}"
            : string.Empty;

        var genreId = ResolveGenreId(genre);
        var genreParam = genreId.HasValue ? $"&g={genreId.Value}&eventcode=CLUB" : "&eventcode=LIVE,CLUB,FEST";

        return $"events/search/?api_key={Uri.EscapeDataString(apiKey)}"
            + keywordParam
            + geoQuery
            + genreParam
            + "&order=date&description=1";
    }

    internal static int? ResolveGenreId(string? genre) =>
        genre?.Trim().ToLowerInvariant() switch
        {
            EventGenres.Techno => 4,
            EventGenres.House => 1,
            EventGenres.DrumAndBass => 7,
            EventGenres.Trance => 5,
            EventGenres.Garage => 26,
            _ => null,
        };

    // Skiddle search API performs broad OR matching across event tags and descriptions.
    // This helper verifies that all query tokens are present in the event name, venue name, or artist line-up.
    internal static bool MatchesQuery(string? query, SkiddleEvent ev)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var artists =
            ev.Artists != null
                ? string.Join(" ", ev.Artists.Where(a => !string.IsNullOrWhiteSpace(a.Name)).Select(a => a.Name))
                : string.Empty;

        var searchable = $"{ev.EventName} {ev.Venue?.Name} {artists}";

        return tokens.All(token => searchable.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    internal static (double Latitude, double Longitude)? ResolveCoordinates(string city) =>
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
