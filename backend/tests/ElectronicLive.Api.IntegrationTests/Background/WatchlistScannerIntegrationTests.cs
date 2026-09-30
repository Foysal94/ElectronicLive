using System.Net;
using ElectronicLive.Api.Background.Scanner;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.IntegrationTests.Infrastructure;
using ElectronicLive.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLive.Api.IntegrationTests.Background;

public class WatchlistScannerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WatchlistScannerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _factory.ResetMocks();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_ExecuteFullLifecycle_WithFingerprintPersistence_Idempotency_AndUnsubscribeHandling()
    {
        const string email = "fan@electroniclive.com";
        const string token = "token-bicep-scanner-lifecycle-12345678901234567890123456789012";
        const string artist = "bicep";
        var subId = Guid.NewGuid();

        // 1. Seed database with active subscriber for artist "Bicep"
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                UnsubscribeToken = token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.Subscriptions.Add(
                new Subscription
                {
                    Id = subId,
                    UserId = user.Id,
                    ArtistName = artist,
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });

        // 2. Stub IEventSearchService returning 2 distinct events in London
        var event1 = new EventResponse(
            Id: "evt-1",
            Name: "Bicep Live at Printworks",
            VenueName: "Printworks",
            Date: new DateOnly(2026, 11, 15),
            Time: new TimeOnly(20, 0),
            TicketUrl: "https://example.com/tickets/bicep-printworks",
            Status: EventStatus.OnSale,
            Provider: EventProvider.Ticketmaster
        );

        var event2 = new EventResponse(
            Id: "evt-2",
            Name: "Bicep DJ Set at Fabric",
            VenueName: "Fabric",
            Date: new DateOnly(2026, 11, 20),
            Time: new TimeOnly(23, 0),
            TicketUrl: "https://example.com/tickets/bicep-fabric",
            Status: EventStatus.OnSale,
            Provider: EventProvider.ResidentAdvisor
        );

        _factory
            .EventSearchService.SearchEventsAsync(artist, null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse> { event1, event2 });

        // 3. Run IWatchlistScannerService.ExecuteScanAsync()
        using (var scope = _factory.Services.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
            var result = await scanner.ExecuteScanAsync();

            result.ArtistsScanned.ShouldBe(1);
            result.SubscriptionsProcessed.ShouldBe(1);
            result.DigestsSent.ShouldBe(1);
            result.ErrorsCount.ShouldBe(0);
        }

        // 4. Assert IEmailDispatcher received exactly 1 digest email with the 2 events and correct unsubscribe link
        await _factory
            .EmailDispatcher.Received(1)
            .SendDigestAsync(
                email,
                artist,
                Arg.Is<IReadOnlyList<EventResponse>>(events =>
                    events.Count == 2 && events.Any(e => e.Id == "evt-1") && events.Any(e => e.Id == "evt-2")
                ),
                Arg.Is<string>(url => url.Contains($"token={token}") && url.Contains($"artist={artist}")),
                Arg.Any<CancellationToken>()
            );

        // 5. Assert NotificationLogs table contains 2 rows with composite fingerprints
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var logs = await db.NotificationLogs.Where(nl => nl.SubscriptionId == subId).ToListAsync();
            logs.Count.ShouldBe(2);

            var fingerprints = logs.Select(l => l.EventFingerprint).ToHashSet();
            fingerprints.ShouldContain("2026-11-15_printworks_bicep");
            fingerprints.ShouldContain("2026-11-20_fabric_bicep");
        });

        // 6. Execute IWatchlistScannerService.ExecuteScanAsync() a second time
        _factory.EmailDispatcher.ClearReceivedCalls();

        using (var scope = _factory.Services.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
            var result = await scanner.ExecuteScanAsync();

            result.ArtistsScanned.ShouldBe(1);
            result.SubscriptionsProcessed.ShouldBe(1);
            result.DigestsSent.ShouldBe(0);
            result.ErrorsCount.ShouldBe(0);
        }

        // 7. Assert IEmailDispatcher received 0 additional emails (idempotency verified)
        await _factory
            .EmailDispatcher.DidNotReceiveWithAnyArgs()
            .SendDigestAsync(default!, default!, default!, default!, default);

        // 8. Deactivate subscription via unsubscribe endpoint
        var unsubResponse = await _client.GetAsync($"/api/subscriptions/unsubscribe?token={token}&artist={artist}");
        unsubResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(subId);
            sub.ShouldNotBeNull();
            sub.IsActive.ShouldBeFalse();
        });

        // 9. Add a new 3rd event to IEventSearchService stub and run scanner again
        var event3 = new EventResponse(
            Id: "evt-3",
            Name: "Bicep at Alexandra Palace",
            VenueName: "Alexandra Palace",
            Date: new DateOnly(2026, 12, 5),
            Time: new TimeOnly(19, 0),
            TicketUrl: "https://example.com/tickets/bicep-allypally",
            Status: EventStatus.OnSale,
            Provider: EventProvider.Skiddle
        );

        _factory
            .EventSearchService.SearchEventsAsync(artist, null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse> { event1, event2, event3 });

        _factory.EmailDispatcher.ClearReceivedCalls();

        using (var scope = _factory.Services.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
            var result = await scanner.ExecuteScanAsync();

            result.ArtistsScanned.ShouldBe(0);
            result.SubscriptionsProcessed.ShouldBe(0);
            result.DigestsSent.ShouldBe(0);
        }

        // 10. Assert 0 emails dispatched for the deactivated subscriber
        await _factory
            .EmailDispatcher.DidNotReceiveWithAnyArgs()
            .SendDigestAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Should_DispatchSeparateEmails_ToMultipleSubscribers_WithIndividualUnsubscribeTokens()
    {
        const string artist = "overmono";
        const string user1Email = "user1@electroniclive.com";
        const string user1Token = "token-user-1-1234567890123456789012345678901234567890";
        const string user2Email = "user2@electroniclive.com";
        const string user2Token = "token-user-2-1234567890123456789012345678901234567890";
        var sub1Id = Guid.NewGuid();
        var sub2Id = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var firstUser = new User
            {
                Id = Guid.NewGuid(),
                Email = user1Email,
                UnsubscribeToken = user1Token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            firstUser.Subscriptions.Add(
                new Subscription
                {
                    Id = sub1Id,
                    UserId = firstUser.Id,
                    ArtistName = artist,
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );

            var secondUser = new User
            {
                Id = Guid.NewGuid(),
                Email = user2Email,
                UnsubscribeToken = user2Token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            secondUser.Subscriptions.Add(
                new Subscription
                {
                    Id = sub2Id,
                    UserId = secondUser.Id,
                    ArtistName = artist,
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );

            db.Users.AddRange(firstUser, secondUser);
            await db.SaveChangesAsync();
        });

        var evt = new EventResponse(
            Id: "evt-om-1",
            Name: "Overmono Live",
            VenueName: "Troxy",
            Date: new DateOnly(2026, 10, 31),
            Time: new TimeOnly(21, 0),
            TicketUrl: "https://example.com/tickets/overmono",
            Status: EventStatus.OnSale,
            Provider: EventProvider.Ticketmaster
        );

        _factory
            .EventSearchService.SearchEventsAsync(artist, null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse> { evt });

        using (var scope = _factory.Services.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
            var result = await scanner.ExecuteScanAsync();

            result.ArtistsScanned.ShouldBe(1);
            result.SubscriptionsProcessed.ShouldBe(2);
            result.DigestsSent.ShouldBe(2);
        }

        await _factory
            .EmailDispatcher.Received(1)
            .SendDigestAsync(
                user1Email,
                artist,
                Arg.Is<IReadOnlyList<EventResponse>>(e => e.Count == 1),
                Arg.Is<string>(url => url.Contains(user1Token)),
                Arg.Any<CancellationToken>()
            );

        await _factory
            .EmailDispatcher.Received(1)
            .SendDigestAsync(
                user2Email,
                artist,
                Arg.Is<IReadOnlyList<EventResponse>>(e => e.Count == 1),
                Arg.Is<string>(url => url.Contains(user2Token)),
                Arg.Any<CancellationToken>()
            );

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var logs = await db.NotificationLogs.ToListAsync();
            logs.Count.ShouldBe(2);
            logs.ShouldContain(l => l.SubscriptionId == sub1Id);
            logs.ShouldContain(l => l.SubscriptionId == sub2Id);
        });
    }

    [Fact]
    public async Task Should_ContinueScanningRemainingArtists_WhenOneArtistFails()
    {
        var fourTetSubId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var firstUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "bicep-fan@electroniclive.com",
                UnsubscribeToken = "token-bicep-fail-12345678901234567890123456789012",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            firstUser.Subscriptions.Add(
                new Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = firstUser.Id,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );

            var secondUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "fourtet-fan@electroniclive.com",
                UnsubscribeToken = "token-fourtet-ok-12345678901234567890123456789012",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            secondUser.Subscriptions.Add(
                new Subscription
                {
                    Id = fourTetSubId,
                    UserId = secondUser.Id,
                    ArtistName = "four tet",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );

            db.Users.AddRange(firstUser, secondUser);
            await db.SaveChangesAsync();
        });

        _factory
            .EventSearchService.SearchEventsAsync("bicep", null, "London", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<EventResponse>>(new HttpRequestException("Upstream timeout")));

        var fourTetEvent = new EventResponse(
            Id: "evt-ft-1",
            Name: "Four Tet All Nighter",
            VenueName: "Brixton Academy",
            Date: new DateOnly(2026, 12, 12),
            Time: new TimeOnly(22, 0),
            TicketUrl: "https://example.com/tickets/four-tet",
            Status: EventStatus.OnSale,
            Provider: EventProvider.ResidentAdvisor
        );

        _factory
            .EventSearchService.SearchEventsAsync("four tet", null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse> { fourTetEvent });

        using (var scope = _factory.Services.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
            var result = await scanner.ExecuteScanAsync();

            result.ArtistsScanned.ShouldBe(2);
            result.DigestsSent.ShouldBe(1);
            result.ErrorsCount.ShouldBe(1);
        }

        await _factory
            .EmailDispatcher.Received(1)
            .SendDigestAsync(
                "fourtet-fan@electroniclive.com",
                "four tet",
                Arg.Is<IReadOnlyList<EventResponse>>(e => e.Count == 1),
                Arg.Is<string>(url => url.Contains("four%20tet")),
                Arg.Any<CancellationToken>()
            );

        await _factory
            .EmailDispatcher.DidNotReceive()
            .SendDigestAsync(
                "bicep-fan@electroniclive.com",
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<EventResponse>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var logs = await db.NotificationLogs.ToListAsync();
            logs.Count.ShouldBe(1);
            logs[0].SubscriptionId.ShouldBe(fourTetSubId);
            logs[0].EventTitle.ShouldBe("Four Tet All Nighter");
        });
    }
}
