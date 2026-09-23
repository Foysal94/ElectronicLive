namespace ElectronicLive.Api.Models;

public static class EventGenres
{
    public const string Techno = "techno";
    public const string House = "house";
    public const string DrumAndBass = "drum-and-bass";
    public const string Trance = "trance";
    public const string Garage = "garage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Techno,
        House,
        DrumAndBass,
        Trance,
        Garage,
    };

    public static bool IsValid(string? genre) => !string.IsNullOrWhiteSpace(genre) && All.Contains(genre.Trim());
}
