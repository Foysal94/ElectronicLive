namespace ElectronicLive.Api.Configuration;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string BaseUrl
    {
        get => field;
        set => field = string.IsNullOrWhiteSpace(value) ? value : $"{value.TrimEnd('/')}/";
    } = "https://api.resend.com/";

    public string ApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = "ElectronicLive <alerts@electroniclive.co.uk>";
}
