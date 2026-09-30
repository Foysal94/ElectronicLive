namespace ElectronicLive.Api.Configuration;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";

    public string BaseUrl { get; set; } = "https://electroniclive.co.uk";

    public int DelayBetweenArtistsMs { get; set; } = 300;
}
