namespace ElectronicLive.Api.Configuration;

public sealed class ResidentAdvisorOptions
{
    public const string SectionName = "EventProviders:ResidentAdvisor";

    public string BaseUrl
    {
        get => field;
        set => field = string.IsNullOrWhiteSpace(value) ? value : $"{value.TrimEnd('/')}/";
    } = "https://ra.co/";

    /// <summary>
    /// RA's Cloudflare perimeter rejects requests with HTTP 403 unless a browser-like User-Agent is supplied.
    /// </summary>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

    public int Limit { get; set; } = 50;
}
