using System.Diagnostics;
using System.Globalization;
using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Background.Scanner;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Background.Scanner;

public sealed class WatchlistScannerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ElectronicLiveDbContext> _options;
    private readonly IEventSearchService _eventSearchService = Substitute.For<IEventSearchService>();
    private readonly IEmailDispatcher _emailDispatcher = Substitute.For<IEmailDispatcher>();

    public WatchlistScannerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        _options = new DbContextOptionsBuilder<ElectronicLiveDbContext>().UseSqlite(_connection).Options;

        using var context = new ElectronicLiveDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private ElectronicLiveDbContext CreateContext() => new(_options);

    private WatchlistScannerService CreateService(int delayMs = 0, string baseUrl = "https://electroniclive.co.uk")
    {
        var scannerOptions = Options.Create(new ScannerOptions { BaseUrl = baseUrl, DelayBetweenArtistsMs = delayMs });
        return new WatchlistScannerService(
            CreateContext(),
            _eventSearchService,
            _emailDispatcher,
            scannerOptions,
            NullLogger<WatchlistScannerService>.Instance
        );
    }

    private async Task<Subscription> SeedSubscriptionAsync(
        string email = "fan@electroniclive.com",
        string artist = "bicep",
        bool isActive = true,
        string token = "test-token-12345678901234567890123456789012"
    )
    {
        using var context = CreateContext();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                UnsubscribeToken = token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            context.Users.Add(user);
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = artist,
            City = "London",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        return subscription;
    }

    private static EventResponse CreateEvent(
        string id = "evt-1",
        string name = "Bicep Live at Drumsheds",
        string venue = "Drumsheds",
        DateOnly? date = null,
        bool useExplicitDate = false
    ) =>
        new(
            id,
            name,
            venue,
            useExplicitDate ? date : (date ?? new DateOnly(2026, 11, 15)),
            new TimeOnly(19, 0),
            "https://ra.co/events/1",
            EventStatus.OnSale,
            EventProvider.ResidentAdvisor
        );

    [Fact]
    public async Task Should_ExecuteScan_DispatchesEmailAndRecordsNotificationLogs_OnFirstRun()
    {
        var subscription = await SeedSubscriptionAsync();
        var gigEvents = new List<EventResponse> { CreateEvent() };

        _eventSearchService.SearchEventsAsync("bicep", null, "London", Arg.Any<CancellationToken>()).Returns(gigEvents);

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(1);
        result.SubscriptionsProcessed.ShouldBe(1);
        result.DigestsSent.ShouldBe(1);
        result.ErrorsCount.ShouldBe(0);

        await _emailDispatcher
            .Received(1)
            .SendDigestAsync(
                "fan@electroniclive.com",
                "bicep",
                Arg.Is<IReadOnlyList<EventResponse>>(l => l.Count == 1 && l[0].Id == "evt-1"),
                "https://electroniclive.co.uk/api/subscriptions/unsubscribe?token=test-token-12345678901234567890123456789012&artist=bicep",
                Arg.Any<CancellationToken>()
            );

        using var verifyContext = CreateContext();
        var savedLogs = await verifyContext.NotificationLogs.ToListAsync();
        savedLogs.Count.ShouldBe(1);
        savedLogs[0].SubscriptionId.ShouldBe(subscription.Id);
        savedLogs[0].EventFingerprint.ShouldBe("2026-11-15_drumsheds_bicep");
        savedLogs[0].EventTitle.ShouldBe("Bicep Live at Drumsheds");
    }

    [Fact]
    public async Task Should_NotDispatchEmail_OnSecondRun_WhenEventsAreAlreadyLogged()
    {
        var subscription = await SeedSubscriptionAsync(artist: "overmono", token: "token-123");

        using (var context = CreateContext())
        {
            context.NotificationLogs.Add(
                new NotificationLog
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscription.Id,
                    EventFingerprint = "2026-12-05_printworks_overmono",
                    EventTitle = "Overmono Live",
                    SentAt = DateTimeOffset.UtcNow.AddDays(-1),
                }
            );
            await context.SaveChangesAsync();
        }

        var gigEvents = new List<EventResponse>
        {
            CreateEvent("evt-1", "Overmono Live", "Printworks", new DateOnly(2026, 12, 5)),
        };

        _eventSearchService
            .SearchEventsAsync("overmono", null, "London", Arg.Any<CancellationToken>())
            .Returns(gigEvents);

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(1);
        result.DigestsSent.ShouldBe(0);
        result.ErrorsCount.ShouldBe(0);

        await _emailDispatcher
            .DidNotReceiveWithAnyArgs()
            .SendDigestAsync(default!, default!, default!, default!, default);

        using var verifyContext = CreateContext();
        (await verifyContext.NotificationLogs.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Should_OnlyDispatchEmailForNewUnsentEvents_WhenSomeEventsAlreadyLogged()
    {
        var subscription = await SeedSubscriptionAsync(artist: "four tet", token: "token-abc");

        using (var context = CreateContext())
        {
            context.NotificationLogs.Add(
                new NotificationLog
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscription.Id,
                    EventFingerprint = "2026-10-01_alexandra palace_four tet",
                    EventTitle = "Four Tet All Nighter",
                    SentAt = DateTimeOffset.UtcNow.AddDays(-2),
                }
            );
            await context.SaveChangesAsync();
        }

        var gigEvents = new List<EventResponse>
        {
            CreateEvent("evt-old", "Four Tet All Nighter", "Alexandra Palace", new DateOnly(2026, 10, 1)),
            CreateEvent("evt-new", "Four Tet at Finsbury Park", "Finsbury Park", new DateOnly(2026, 10, 20)),
        };

        _eventSearchService
            .SearchEventsAsync("four tet", null, "London", Arg.Any<CancellationToken>())
            .Returns(gigEvents);

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.DigestsSent.ShouldBe(1);

        await _emailDispatcher
            .Received(1)
            .SendDigestAsync(
                "fan@electroniclive.com",
                "four tet",
                Arg.Is<IReadOnlyList<EventResponse>>(l => l.Count == 1 && l[0].Id == "evt-new"),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        using var verifyContext = CreateContext();
        var savedLogs = await verifyContext.NotificationLogs.ToListAsync();
        savedLogs.Count.ShouldBe(2);
        savedLogs.ShouldContain(l => l.EventFingerprint == "2026-10-20_finsbury park_four tet");
    }

    [Fact]
    public async Task Should_HandleEmptySubscriptionsOrNoEvents_Gracefully()
    {
        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(0);
        result.SubscriptionsProcessed.ShouldBe(0);
        result.DigestsSent.ShouldBe(0);
        result.ErrorsCount.ShouldBe(0);

        await _emailDispatcher
            .DidNotReceiveWithAnyArgs()
            .SendDigestAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Should_IgnoreInactiveSubscriptions()
    {
        await SeedSubscriptionAsync(artist: "bonobo", isActive: false);

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(0);
        result.DigestsSent.ShouldBe(0);
        await _eventSearchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Should_IsolateSubscriberFailure_AndContinueProcessingOtherSubscribersAndArtists()
    {
        await SeedSubscriptionAsync("failing@test.com", "fred again..", true, "tok-1");
        var sub2 = await SeedSubscriptionAsync("success@test.com", "fred again..", true, "tok-2");

        _eventSearchService
            .SearchEventsAsync("fred again..", null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse> { CreateEvent("evt-1", "Fred again.. Live", "Brixton Academy") });

        _emailDispatcher
            .SendDigestAsync(
                "failing@test.com",
                "fred again..",
                Arg.Any<IReadOnlyList<EventResponse>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Resend API down"));

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(1);
        result.SubscriptionsProcessed.ShouldBe(2);
        result.DigestsSent.ShouldBe(1);
        result.ErrorsCount.ShouldBe(1);

        await _emailDispatcher
            .Received(1)
            .SendDigestAsync(
                "success@test.com",
                "fred again..",
                Arg.Any<IReadOnlyList<EventResponse>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        using var verifyContext = CreateContext();
        var savedLogs = await verifyContext.NotificationLogs.ToListAsync();
        savedLogs.Count.ShouldBe(1);
        savedLogs[0].SubscriptionId.ShouldBe(sub2.Id);
    }

    [Fact]
    public async Task Should_IsolateEventSearchFailure_AndContinueProcessingOtherArtists()
    {
        await SeedSubscriptionAsync(artist: "broken-artist", token: "tok-1");
        await SeedSubscriptionAsync(artist: "working-artist", token: "tok-2");

        _eventSearchService
            .SearchEventsAsync("broken-artist", null, "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Provider timeout"));

        _eventSearchService
            .SearchEventsAsync("working-artist", null, "London", Arg.Any<CancellationToken>())
            .Returns(
                new List<EventResponse>
                {
                    CreateEvent("evt-ok", "Working Artist Live", "Village Underground", new DateOnly(2026, 12, 1)),
                }
            );

        var service = CreateService();
        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(2);
        result.DigestsSent.ShouldBe(1);
        result.ErrorsCount.ShouldBe(1);

        await _emailDispatcher
            .Received(1)
            .SendDigestAsync(
                "fan@electroniclive.com",
                "working-artist",
                Arg.Any<IReadOnlyList<EventResponse>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Should_RespectDelayBetweenArtists()
    {
        await SeedSubscriptionAsync(artist: "artist-one", token: "tok-1");
        await SeedSubscriptionAsync(artist: "artist-two", token: "tok-2");

        _eventSearchService
            .SearchEventsAsync(Arg.Any<string>(), null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse>());

        var service = CreateService(delayMs: 50);

        var stopwatch = Stopwatch.StartNew();
        var result = await service.ExecuteScanAsync();
        stopwatch.Stop();

        result.ArtistsScanned.ShouldBe(2);
        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(40);
    }

    [Theory]
    [InlineData("2026-11-15", "Drumsheds ", " Bicep", "2026-11-15_drumsheds_bicep")]
    [InlineData(null, "The Roundhouse", "Bonobo", "tba_the roundhouse_bonobo")]
    public void Should_BuildCorrectCompositeFingerprint(
        string? dateString,
        string venue,
        string artist,
        string expectedFingerprint
    )
    {
        DateOnly? date = dateString != null ? DateOnly.Parse(dateString, CultureInfo.InvariantCulture) : null;
        var evt = CreateEvent(venue: venue, date: date, useExplicitDate: true);

        var fingerprint = WatchlistScannerService.BuildEventFingerprint(evt, artist);

        fingerprint.ShouldBe(expectedFingerprint);
    }
}
