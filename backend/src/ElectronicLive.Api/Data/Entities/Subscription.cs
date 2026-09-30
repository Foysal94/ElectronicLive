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
}
