namespace ElectronicLive.Api.Configuration;

public sealed class SkiddleOptions
{
    public const string SectionName = "Skiddle";

    public string BaseUrl
    {
        get => field;
        set => field = string.IsNullOrWhiteSpace(value) ? value : $"{value.TrimEnd('/')}/";
    } = "https://www.skiddle.com/api/v1/";

    public string ApiKey { get; set; } = string.Empty;
}
