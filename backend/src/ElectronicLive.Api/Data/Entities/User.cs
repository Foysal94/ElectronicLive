namespace ElectronicLive.Api.Data.Entities;

public class User
{
    private string _email = string.Empty;

    public Guid Id { get; set; }

    public string Email
    {
        get => _email;
        set => _email = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public string UnsubscribeToken { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
