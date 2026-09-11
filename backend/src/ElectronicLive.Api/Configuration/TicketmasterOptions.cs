namespace ElectronicLive.Api.Configuration;

public sealed class TicketmasterOptions
{
    public const string SectionName = "Ticketmaster";

    public string BaseUrl { get; set; } = "https://app.ticketmaster.com/discovery/v2/";
    public string ApiKey { get; set; } = string.Empty;
}
