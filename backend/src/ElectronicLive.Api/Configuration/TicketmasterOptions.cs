namespace ElectronicLive.Api.Configuration;

public sealed class TicketmasterOptions
{
    public const string SectionName = "EventProviders:Ticketmaster";

    public string BaseUrl
    {
        get => field;
        set => field = string.IsNullOrWhiteSpace(value) ? value : $"{value.TrimEnd('/')}/";
    } = "https://app.ticketmaster.com/discovery/v2/";
    public string ApiKey { get; set; } = string.Empty;
}
