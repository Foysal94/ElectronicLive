using System.Globalization;
using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Background.Scanning;

public sealed class WatchlistScannerService(
    ElectronicLiveDbContext dbContext,
    IEventSearchService eventSearchService,
    IEmailDispatcher emailDispatcher,
    IOptions<ScannerOptions> options,
    ILogger<WatchlistScannerService> logger
) : IWatchlistScannerService
{
    public async Task<ScanResult> ExecuteScanAsync(CancellationToken ct = default)
    {
        var watchedArtists = await dbContext
            .Subscriptions.AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => s.ArtistName)
            .Distinct()
            .ToListAsync(ct);

        logger.LogInformation(
            "Found {Count} distinct watched artists across active subscriptions.",
            watchedArtists.Count
        );

        var artistsScanned = 0;
        var subscriptionsProcessed = 0;
        var digestsSent = 0;
        var errorsCount = 0;

        var delayMs = options.Value.DelayBetweenArtistsMs;
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');

        for (var i = 0; i < watchedArtists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var artist = watchedArtists[i];
            artistsScanned++;

            if (i > 0 && delayMs > 0)
            {
                await Task.Delay(delayMs, ct);
            }

            logger.LogInformation(
                "Scanning artist {Current}/{Total}: '{Artist}' in London...",
                i + 1,
                watchedArtists.Count,
                artist
            );

            IReadOnlyList<EventResponse> events;
            try
            {
                events = await eventSearchService.SearchEventsAsync(artist, null, "London", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to search events for artist '{Artist}'. Skipping artist.", artist);
                errorsCount++;
                continue;
            }

            if (events.Count == 0)
            {
                logger.LogInformation("No events found in London for artist '{Artist}'.", artist);
                continue;
            }

            var subscriptions = await dbContext
                .Subscriptions.Include(s => s.User)
                .Include(s => s.NotificationLogs)
                .Where(s => s.IsActive && s.ArtistName == artist)
                .ToListAsync(ct);

            foreach (var subscription in subscriptions)
            {
                ct.ThrowIfCancellationRequested();
                subscriptionsProcessed++;

                if (subscription.User == null || string.IsNullOrWhiteSpace(subscription.User.Email))
                {
                    continue;
                }

                var existingFingerprints = subscription
                    .NotificationLogs.Select(nl => nl.EventFingerprint)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var unsentEvents = new List<(EventResponse Event, string Fingerprint)>();
                foreach (var evt in events)
                {
                    var fingerprint = BuildEventFingerprint(evt, artist);
                    if (!existingFingerprints.Contains(fingerprint))
                    {
                        unsentEvents.Add((evt, fingerprint));
                    }
                }

                if (unsentEvents.Count == 0)
                {
                    continue;
                }

                var newEventsList = unsentEvents.Select(u => u.Event).ToList();
                var encodedArtist = Uri.EscapeDataString(artist);
                var unsubscribeUrl =
                    $"{baseUrl}/api/subscriptions/unsubscribe?token={subscription.User.UnsubscribeToken}&artist={encodedArtist}";

                try
                {
                    await emailDispatcher.SendDigestAsync(
                        subscription.User.Email,
                        artist,
                        newEventsList,
                        unsubscribeUrl,
                        ct
                    );

                    var newLogs = unsentEvents.Select(u => new NotificationLog
                    {
                        Id = Guid.NewGuid(),
                        SubscriptionId = subscription.Id,
                        EventFingerprint = u.Fingerprint,
                        EventTitle = u.Event.Name,
                        SentAt = DateTimeOffset.UtcNow,
                    });

                    dbContext.NotificationLogs.AddRange(newLogs);
                    await dbContext.SaveChangesAsync(ct);

                    digestsSent++;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to send digest or save notification log for subscription {SubscriptionId} (User: {Email}, Artist: {Artist}).",
                        subscription.Id,
                        subscription.User.Email,
                        artist
                    );
                    errorsCount++;
                }
            }
        }

        return new ScanResult(
            ArtistsScanned: artistsScanned,
            SubscriptionsProcessed: subscriptionsProcessed,
            DigestsSent: digestsSent,
            ErrorsCount: errorsCount
        );
    }

    public static string BuildEventFingerprint(EventResponse evt, string artist)
    {
        var datePart = evt.Date.HasValue ? evt.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "tba";
        var venuePart = evt.VenueName.Trim().ToLowerInvariant();
        var artistPart = artist.Trim().ToLowerInvariant();

        return $"{datePart}_{venuePart}_{artistPart}";
    }
}
