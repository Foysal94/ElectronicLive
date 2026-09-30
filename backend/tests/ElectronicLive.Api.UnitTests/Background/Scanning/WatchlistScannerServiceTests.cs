using System.Globalization;
using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Background.Scanning;
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

namespace ElectronicLive.Api.UnitTests.Background.Scanning;

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

    private WatchlistScannerService CreateService(
        ElectronicLiveDbContext dbContext,
        int delayMs = 0,
        string baseUrl = "https://electroniclive.co.uk"
    )
    {
        var scannerOptions = Options.Create(new ScannerOptions { BaseUrl = baseUrl, DelayBetweenArtistsMs = delayMs });

        return new WatchlistScannerService(
            dbContext,
            _eventSearchService,
            _emailDispatcher,
            scannerOptions,
            NullLogger<WatchlistScannerService>.Instance
        );
    }

    [Fact]
    public async Task Should_ExecuteScan_DispatchesEmailAndRecordsNotificationLogs_OnFirstRun()
    {
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "test-token-12345678901234567890123456789012",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "bicep",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var gigEvents = new List<EventResponse>
        {
            new(
                "evt-1",
                "Bicep Live at Drumsheds",
                "Drumsheds",
                new DateOnly(2026, 11, 15),
                new TimeOnly(19, 0),
                "https://ra.co/events/1",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };

        _eventSearchService.SearchEventsAsync("bicep", null, "London", Arg.Any<CancellationToken>()).Returns(gigEvents);

        var service = CreateService(dbContext);

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
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "token-123",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "overmono",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var existingLog = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            EventFingerprint = "2026-12-05_printworks_overmono",
            EventTitle = "Overmono Live",
            SentAt = DateTimeOffset.UtcNow.AddDays(-1),
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.Add(subscription);
        dbContext.NotificationLogs.Add(existingLog);
        await dbContext.SaveChangesAsync();

        var gigEvents = new List<EventResponse>
        {
            new(
                "evt-1",
                "Overmono Live",
                "Printworks",
                new DateOnly(2026, 12, 5),
                null,
                "https://skiddle.com/1",
                EventStatus.OnSale,
                EventProvider.Skiddle
            ),
        };

        _eventSearchService
            .SearchEventsAsync("overmono", null, "London", Arg.Any<CancellationToken>())
            .Returns(gigEvents);

        var service = CreateService(dbContext);

        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(1);
        result.SubscriptionsProcessed.ShouldBe(1);
        result.DigestsSent.ShouldBe(0);
        result.ErrorsCount.ShouldBe(0);

        await _emailDispatcher
            .DidNotReceiveWithAnyArgs()
            .SendDigestAsync(default!, default!, default!, default!, default);

        using var verifyContext = CreateContext();
        var logsCount = await verifyContext.NotificationLogs.CountAsync();
        logsCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_OnlyDispatchEmailForNewUnsentEvents_WhenSomeEventsAlreadyLogged()
    {
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "token-abc",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "four tet",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var existingLog = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            EventFingerprint = "2026-10-01_alexandra palace_four tet",
            EventTitle = "Four Tet All Nighter",
            SentAt = DateTimeOffset.UtcNow.AddDays(-2),
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.Add(subscription);
        dbContext.NotificationLogs.Add(existingLog);
        await dbContext.SaveChangesAsync();

        var gigEvents = new List<EventResponse>
        {
            new(
                "evt-old",
                "Four Tet All Nighter",
                "Alexandra Palace",
                new DateOnly(2026, 10, 1),
                null,
                "https://ra.co/1",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
            new(
                "evt-new",
                "Four Tet at Finsbury Park",
                "Finsbury Park",
                new DateOnly(2026, 10, 20),
                null,
                "https://ticketmaster.co.uk/1",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };

        _eventSearchService
            .SearchEventsAsync("four tet", null, "London", Arg.Any<CancellationToken>())
            .Returns(gigEvents);

        var service = CreateService(dbContext);

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
        using var dbContext = CreateContext();

        var service = CreateService(dbContext);

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
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "token-inactive",
        };
        var inactiveSub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "bonobo",
            IsActive = false,
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.Add(inactiveSub);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.ExecuteScanAsync();

        result.ArtistsScanned.ShouldBe(0);
        result.DigestsSent.ShouldBe(0);
        await _eventSearchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Should_IsolateSubscriberFailure_AndContinueProcessingOtherSubscribersAndArtists()
    {
        using var dbContext = CreateContext();

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "failing@test.com",
            UnsubscribeToken = "tok-1",
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "success@test.com",
            UnsubscribeToken = "tok-2",
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            User = user1,
            ArtistName = "fred again..",
            IsActive = true,
        };
        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            User = user2,
            ArtistName = "fred again..",
            IsActive = true,
        };

        dbContext.Users.AddRange(user1, user2);
        dbContext.Subscriptions.AddRange(sub1, sub2);
        await dbContext.SaveChangesAsync();

        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Fred again.. Live",
                "Brixton Academy",
                new DateOnly(2026, 11, 1),
                null,
                "https://ticketmaster.com/1",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };

        _eventSearchService
            .SearchEventsAsync("fred again..", null, "London", Arg.Any<CancellationToken>())
            .Returns(events);

        _emailDispatcher
            .SendDigestAsync(
                "failing@test.com",
                "fred again..",
                Arg.Any<IReadOnlyList<EventResponse>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Resend API down"));

        var service = CreateService(dbContext);

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
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "tok-1",
        };
        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "broken-artist",
            IsActive = true,
        };
        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "working-artist",
            IsActive = true,
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.AddRange(sub1, sub2);
        await dbContext.SaveChangesAsync();

        _eventSearchService
            .SearchEventsAsync("broken-artist", null, "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Provider timeout"));

        _eventSearchService
            .SearchEventsAsync("working-artist", null, "London", Arg.Any<CancellationToken>())
            .Returns(
                new List<EventResponse>
                {
                    new(
                        "evt-ok",
                        "Working Artist Live",
                        "Village Underground",
                        new DateOnly(2026, 12, 1),
                        null,
                        "https://ra.co/2",
                        EventStatus.OnSale,
                        EventProvider.ResidentAdvisor
                    ),
                }
            );

        var service = CreateService(dbContext);

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
        using var dbContext = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan@electroniclive.com",
            UnsubscribeToken = "tok",
        };
        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "artist-one",
            IsActive = true,
        };
        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ArtistName = "artist-two",
            IsActive = true,
        };

        dbContext.Users.Add(user);
        dbContext.Subscriptions.AddRange(sub1, sub2);
        await dbContext.SaveChangesAsync();

        _eventSearchService
            .SearchEventsAsync(Arg.Any<string>(), null, "London", Arg.Any<CancellationToken>())
            .Returns(new List<EventResponse>());

        var service = CreateService(dbContext, delayMs: 50);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
        var evt = new EventResponse(
            "test-id",
            "Event Name",
            venue,
            date,
            null,
            "https://test.com",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );

        var fingerprint = WatchlistScannerService.BuildEventFingerprint(evt, artist);

        fingerprint.ShouldBe(expectedFingerprint);
    }
}
