namespace ElectronicLive.Api.Data.Entities;

public class Subscription
{
    private string _artistName = string.Empty;

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string ArtistName
    {
        get => _artistName;
        set => _artistName = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public string City { get; set; } = "London";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];

    public string BuildUnsubscribeUrl(string baseUrl)
    {
        var token = User?.UnsubscribeToken ?? string.Empty;
        var encodedArtist = Uri.EscapeDataString(ArtistName);
        return $"{baseUrl.TrimEnd('/')}/api/subscriptions/unsubscribe?token={token}&artist={encodedArtist}";
    }

    public static string BuildUnsubscribeUrl(string baseUrl, string token, string artist)
    {
        var encodedArtist = Uri.EscapeDataString(artist.Trim().ToLowerInvariant());
        return $"{baseUrl.TrimEnd('/')}/api/subscriptions/unsubscribe?token={token.Trim()}&artist={encodedArtist}";
    }
}
