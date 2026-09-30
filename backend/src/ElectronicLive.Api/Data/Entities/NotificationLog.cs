using System.Globalization;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Data.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    public string EventFingerprint { get; set; } = string.Empty;

    public string EventTitle { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public static string GenerateFingerprint(EventResponse evt, string artist)
    {
        var datePart = evt.Date.HasValue ? evt.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "tba";
        var venuePart = evt.VenueName.Trim().ToLowerInvariant();
        var artistPart = artist.Trim().ToLowerInvariant();

        return $"{datePart}_{venuePart}_{artistPart}";
    }
}
