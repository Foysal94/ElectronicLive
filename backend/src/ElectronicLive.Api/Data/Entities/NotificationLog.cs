namespace ElectronicLive.Api.Data.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    public string EventFingerprint { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
