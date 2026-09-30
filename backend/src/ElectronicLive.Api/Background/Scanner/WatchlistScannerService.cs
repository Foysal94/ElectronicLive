using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Background.Scanner;

public sealed class WatchlistScannerService(
    ElectronicLiveDbContext dbContext,
    IEventSearchService eventSearchService,
    IEmailDispatcher emailDispatcher,
    IOptions<ScannerOptions> options,
    ILogger<WatchlistScannerService> logger
) : IWatchlistScannerService
{
    private const string TargetCity = "London";

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

        var totalResult = ScanResult.Empty;
        var delayMs = options.Value.DelayBetweenArtistsMs;
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');

        for (var i = 0; i < watchedArtists.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var artist = watchedArtists[i];

            if (i > 0 && delayMs > 0)
            {
                await Task.Delay(delayMs, ct);
            }

            logger.LogInformation(
                "Scanning artist {Current}/{Total}: '{Artist}' in {City}...",
                i + 1,
                watchedArtists.Count,
                artist,
                TargetCity
            );

            var artistResult = await ProcessArtistAsync(artist, baseUrl, ct);
            totalResult += artistResult;
        }

        return totalResult;
    }

    private async Task<ArtistScanResult> ProcessArtistAsync(string artist, string baseUrl, CancellationToken ct)
    {
        IReadOnlyList<EventResponse> events;
        try
        {
            events = await eventSearchService.SearchEventsAsync(artist, null, TargetCity, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search events for artist '{Artist}'. Skipping artist.", artist);
            return ArtistScanResult.Failed;
        }

        if (events.Count == 0)
        {
            logger.LogInformation("No events found in {City} for artist '{Artist}'.", TargetCity, artist);
            return ArtistScanResult.Empty;
        }

        var subscriptions = await dbContext
            .Subscriptions.Include(s => s.User)
            .Include(s => s.NotificationLogs)
            .Where(s => s.IsActive && s.ArtistName == artist)
            .ToListAsync(ct);

        var processedCount = 0;
        var sentCount = 0;
        var errorCount = 0;

        foreach (var subscription in subscriptions)
        {
            ct.ThrowIfCancellationRequested();
            processedCount++;

            if (subscription.User == null || string.IsNullOrWhiteSpace(subscription.User.Email))
            {
                continue;
            }

            var outcome = await ProcessSubscriptionAsync(subscription, artist, events, baseUrl, ct);
            switch (outcome)
            {
                case SubscriptionProcessOutcome.Sent:
                    sentCount++;
                    break;
                case SubscriptionProcessOutcome.Failed:
                    errorCount++;
                    break;
                case SubscriptionProcessOutcome.NoNewEvents:
                    break;
            }
        }

        return new ArtistScanResult(processedCount, sentCount, errorCount);
    }

    private async Task<SubscriptionProcessOutcome> ProcessSubscriptionAsync(
        Subscription subscription,
        string artist,
        IReadOnlyList<EventResponse> events,
        string baseUrl,
        CancellationToken ct
    )
    {
        var existingFingerprints = subscription
            .NotificationLogs.Select(nl => nl.EventFingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unsentEvents = new List<(EventResponse Event, string Fingerprint)>();
        foreach (var evt in events)
        {
            var fingerprint = NotificationLog.GenerateFingerprint(evt, artist);
            if (!existingFingerprints.Contains(fingerprint))
            {
                unsentEvents.Add((evt, fingerprint));
            }
        }

        if (unsentEvents.Count == 0)
        {
            return SubscriptionProcessOutcome.NoNewEvents;
        }

        var newEventsList = unsentEvents.Select(u => u.Event).ToList();
        var unsubscribeUrl = subscription.BuildUnsubscribeUrl(baseUrl);

        try
        {
            await emailDispatcher.SendDigestAsync(subscription.User!.Email, artist, newEventsList, unsubscribeUrl, ct);

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

            return SubscriptionProcessOutcome.Sent;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send digest or save notification log for subscription {SubscriptionId} (User: {Email}, Artist: {Artist}).",
                subscription.Id,
                subscription.User!.Email,
                artist
            );
            return SubscriptionProcessOutcome.Failed;
        }
    }
}
