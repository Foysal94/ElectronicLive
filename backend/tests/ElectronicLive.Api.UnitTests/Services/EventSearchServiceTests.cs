using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventSearchServiceTests
{
    private readonly ILogger<EventSearchService> _logger = Substitute.For<ILogger<EventSearchService>>();
    private readonly HybridCache _cache;

    public EventSearchServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();
    }

    private EventSearchService CreateService(IEnumerable<IEventProvider> providers) => new(providers, _cache, _logger);

    private static EventResponse CreateSampleEvent(
        string id = "e-1",
        string name = "Bicep Live",
        string venue = "The Drumsheds",
        DateOnly? date = null,
        TimeOnly? time = null,
        EventStatus status = EventStatus.OnSale,
        EventProvider provider = EventProvider.Ticketmaster,
        string? ticketUrl = "https://tickets.com/1",
        bool explicitDate = false
    ) =>
        new(
            id,
            name,
            venue,
            explicitDate ? date : (date ?? new DateOnly(2026, 12, 10)),
            time ?? new TimeOnly(21, 0),
            ticketUrl,
            status,
            provider
        );

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "")]
    [InlineData("", null)]
    public async Task Should_ReturnEmptyList_WhenQueryAndGenreAreWhitespace(string? query, string? genre)
    {
        var provider = Substitute.For<IEventProvider>();
        var service = CreateService([provider]);

        var result = await service.SearchEventsAsync(query, genre, "London");

        result.ShouldBeEmpty();
        await provider
            .DidNotReceive()
            .SearchEventsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenNoProvidersRegistered()
    {
        var service = CreateService([]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_QueryAllProvidersConcurrently_AndAggregateResults()
    {
        var provider1 = Substitute.For<IEventProvider>();
        provider1.Provider.Returns(EventProvider.Ticketmaster);
        var event1 = CreateSampleEvent(
            "p1-1",
            "Bicep Live",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(20, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        provider1
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([event1]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.Provider.Returns(EventProvider.Skiddle);
        var event2 = CreateSampleEvent(
            "p2-1",
            "Bicep DJ Set",
            "Venue 2",
            new DateOnly(2026, 11, 22),
            new TimeOnly(22, 0),
            EventStatus.OnSale,
            EventProvider.Skiddle
        );
        provider2
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([event2]);

        var service = CreateService([provider1, provider2]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(event1.Id);
        result[0].Offers.ShouldNotBeNull().Count.ShouldBe(1);
        result[1].Id.ShouldBe(event2.Id);
        result[1].Offers.ShouldNotBeNull().Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_AggregateResults_WhenQueryingByVenue()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var venueEvent = CreateSampleEvent(
            "v-1",
            "Saturday Night Session",
            "fabric",
            new DateOnly(2026, 11, 28),
            new TimeOnly(23, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        provider
            .SearchEventsAsync("fabric", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([venueEvent]);

        var service = CreateService([provider]);

        var result = await service.SearchEventsAsync("fabric", null, "London");

        result.ShouldHaveSingleItem().VenueName.ShouldBe("fabric");
        await provider
            .Received(1)
            .SearchEventsAsync("fabric", null, "London", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PopulateSingleOffer_WhenEventHasNoDuplicates()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var singleEvent = CreateSampleEvent(
            "tm-1",
            "Fred Again..",
            "Alexandra Palace",
            new DateOnly(2026, 9, 30),
            new TimeOnly(19, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/fred"
        );
        provider
            .SearchEventsAsync("Fred Again..", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([singleEvent]);

        var service = CreateService([provider]);

        var result = await service.SearchEventsAsync("Fred Again..", null, "London");

        var item = result.ShouldHaveSingleItem();
        var offers = item.Offers.ShouldNotBeNull();
        var offer = offers.ShouldHaveSingleItem();
        offer.Provider.ShouldBe(EventProvider.Ticketmaster);
        offer.TicketUrl.ShouldBe("https://ticketmaster.com/fred");
        offer.Status.ShouldBe(EventStatus.OnSale);
    }

    [Fact]
    public async Task Should_MergeDuplicateEventsAcrossProviders_AndCombineOffers()
    {
        var tmProvider = Substitute.For<IEventProvider>();
        tmProvider.Provider.Returns(EventProvider.Ticketmaster);
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Four Tet Live",
            "The Drumsheds",
            new DateOnly(2026, 10, 17),
            new TimeOnly(14, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/1"
        );
        tmProvider
            .SearchEventsAsync("Four Tet", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([tmEvent]);

        var skProvider = Substitute.For<IEventProvider>();
        skProvider.Provider.Returns(EventProvider.Skiddle);
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Four Tet - All Day Long",
            "Drumsheds, London",
            new DateOnly(2026, 10, 17),
            new TimeOnly(13, 0),
            EventStatus.SoldOut,
            EventProvider.Skiddle,
            "https://skiddle.com/1"
        );
        skProvider
            .SearchEventsAsync("Four Tet", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([skEvent]);

        var service = CreateService([tmProvider, skProvider]);

        var result = await service.SearchEventsAsync("Four Tet", null, "London");

        var merged = result.ShouldHaveSingleItem();
        merged.Date.ShouldBe(new DateOnly(2026, 10, 17));
        merged.Time.ShouldBe(new TimeOnly(13, 0));
        merged.Status.ShouldBe(EventStatus.OnSale);
        merged.Provider.ShouldBe(EventProvider.Ticketmaster);

        var offers = merged.Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_OrderAggregatedEventsByDateThenTime_AndPushTbaToEnd()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var eventLaterDate = CreateSampleEvent(
            "3",
            "Event Later Date",
            "Venue 3",
            new DateOnly(2026, 12, 1),
            new TimeOnly(20, 0)
        );
        var eventEarlierTime = CreateSampleEvent(
            "1",
            "Event Earlier Time",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(18, 0)
        );
        var eventLaterTime = CreateSampleEvent(
            "2",
            "Event Later Time",
            "Venue 2",
            new DateOnly(2026, 11, 20),
            new TimeOnly(22, 0)
        );
        var eventTba = CreateSampleEvent("4", "Event TBA Date", "Venue 4", null, null, explicitDate: true);

        provider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([eventLaterDate, eventTba, eventLaterTime, eventEarlierTime]);

        var service = CreateService([provider]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.Count.ShouldBe(4);
        result[0].Id.ShouldBe("1");
        result[1].Id.ShouldBe("2");
        result[2].Id.ShouldBe("3");
        result[3].Id.ShouldBe("4");
    }

    [Fact]
    public async Task Should_ExecuteFactory_OnCacheMiss_AndServeSubsequentRequests_FromCache()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var sampleEvent = CreateSampleEvent("1", "Bicep Live", "Drumsheds");
        provider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([sampleEvent]);

        var service = CreateService([provider]);

        var result1 = await service.SearchEventsAsync("Bicep", null, "London");
        var result2 = await service.SearchEventsAsync("bicep", null, "LONDON");

        result1.Count.ShouldBe(1);
        result2.Count.ShouldBe(1);
        await provider
            .Received(1)
            .SearchEventsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Should_NormalizeCacheKeys_AcrossCasingAndWhitespace()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var sampleEvent = CreateSampleEvent("1", "Bicep Live", "Drumsheds");
        provider
            .SearchEventsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([sampleEvent]);

        var service = CreateService([provider]);

        var r1 = await service.SearchEventsAsync("  Bicep  ", "  Techno  ", " London ");
        var r2 = await service.SearchEventsAsync("bicep", "techno", "london");
        var r3 = await service.SearchEventsAsync("BICEP", "TECHNO", "LONDON");

        r1.Count.ShouldBe(1);
        r2.Count.ShouldBe(1);
        r3.Count.ShouldBe(1);
        await provider
            .Received(1)
            .SearchEventsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<DateOnly?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Should_GenerateUnambiguousCacheKeys_WhenQueryAndGenreBothExist()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var bicepEvent = CreateSampleEvent("1", "Bicep Live");
        var technoEvent = CreateSampleEvent("2", "Techno Night");
        var bicepTechnoEvent = CreateSampleEvent("3", "Bicep Techno");

        provider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([bicepEvent]);
        provider
            .SearchEventsAsync(null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([technoEvent]);
        provider
            .SearchEventsAsync("Bicep", "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([bicepTechnoEvent]);

        var service = CreateService([provider]);

        var r1 = await service.SearchEventsAsync("Bicep", null, "London");
        var r2 = await service.SearchEventsAsync(null, "techno", "London");
        var r3 = await service.SearchEventsAsync("Bicep", "techno", "London");

        r1.ShouldHaveSingleItem().Id.ShouldBe("1");
        r2.ShouldHaveSingleItem().Id.ShouldBe("2");
        r3.ShouldHaveSingleItem().Id.ShouldBe("3");
    }

    [Fact]
    public async Task Should_NotCache_WhenAllProvidersFail_AllowingSubsequentRetries()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        provider
            .SearchEventsAsync("Overmono", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Temporary outage"));

        var service = CreateService([provider]);

        await Should.ThrowAsync<AllProvidersUnavailableException>(() =>
            service.SearchEventsAsync("Overmono", null, "London")
        );

        // Next call recovers and should invoke provider again instead of returning cached failure
        var recoveredEvent = CreateSampleEvent("e-2", "Overmono Live");
        provider
            .SearchEventsAsync("Overmono", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([recoveredEvent]);

        var retryResult = await service.SearchEventsAsync("Overmono", null, "London");

        retryResult.ShouldHaveSingleItem().Id.ShouldBe("e-2");
    }

    [Fact]
    public async Task Should_IsolateProviderFailure_WhenOneProviderThrows()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.Provider.Returns(EventProvider.Ticketmaster);
        failingProvider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("500 Internal Server Error"));

        var healthyProvider = Substitute.For<IEventProvider>();
        healthyProvider.Provider.Returns(EventProvider.Skiddle);
        var healthyEvent = CreateSampleEvent(
            "h-1",
            "Bicep Live",
            "Venue H",
            new DateOnly(2026, 11, 25),
            new TimeOnly(20, 0),
            EventStatus.OnSale,
            EventProvider.Skiddle
        );
        healthyProvider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([healthyEvent]);

        var service = CreateService([failingProvider, healthyProvider]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(healthyEvent.Id);
    }

    [Fact]
    public async Task Should_IsolateProviderTimeout_WhenCallerHasNotCancelled()
    {
        var timingOutProvider = Substitute.For<IEventProvider>();
        timingOutProvider.Provider.Returns(EventProvider.Ticketmaster);
        timingOutProvider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("HttpClient timeout"));

        var healthyProvider = Substitute.For<IEventProvider>();
        healthyProvider.Provider.Returns(EventProvider.Skiddle);
        var healthyEvent = CreateSampleEvent(
            "h-1",
            "Bicep Live",
            "Venue H",
            new DateOnly(2026, 11, 25),
            new TimeOnly(20, 0),
            EventStatus.OnSale,
            EventProvider.Skiddle
        );
        healthyProvider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([healthyEvent]);

        var service = CreateService([timingOutProvider, healthyProvider]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(healthyEvent.Id);
    }

    [Fact]
    public async Task Should_ThrowAllProvidersUnavailableException_WhenAllProvidersFail()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.Provider.Returns(EventProvider.Ticketmaster);
        failingProvider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Upstream exploded"));

        var service = CreateService([failingProvider]);

        var exception = await Should.ThrowAsync<AllProvidersUnavailableException>(() =>
            service.SearchEventsAsync("Bicep", null, "London")
        );
        exception.Query.ShouldBe("Bicep");
        exception.ProviderCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_HandleProviderReturningNullListGracefully()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        provider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventResponse>)null!);

        var service = CreateService([provider]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_PropagateOperationCanceledException_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = Substitute.For<IEventProvider>();
        provider
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: cts.Token)
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var service = CreateService([provider]);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.SearchEventsAsync("Bicep", null, "London", cancellationToken: cts.Token)
        );
    }

    [Fact]
    public async Task Should_PassCancellationTokenToAllProviders()
    {
        using var cts = new CancellationTokenSource();

        var provider1 = Substitute.For<IEventProvider>();
        provider1.SearchEventsAsync("Bicep", null, "London", cancellationToken: cts.Token).Returns([]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.SearchEventsAsync("Bicep", null, "London", cancellationToken: cts.Token).Returns([]);

        var service = CreateService([provider1, provider2]);

        await service.SearchEventsAsync("Bicep", null, "London", cancellationToken: cts.Token);

        await provider1
            .Received(1)
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>());
        await provider2
            .Received(1)
            .SearchEventsAsync("Bicep", null, "London", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ForwardGenreParameterToAllProvidersConcurrently()
    {
        var provider1 = Substitute.For<IEventProvider>();
        provider1.Provider.Returns(EventProvider.Ticketmaster);
        provider1
            .SearchEventsAsync(null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.Provider.Returns(EventProvider.ResidentAdvisor);
        provider2
            .SearchEventsAsync(null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService([provider1, provider2]);

        var result = await service.SearchEventsAsync(null, "techno", "London");

        result.ShouldBeEmpty();
        await provider1
            .Received(1)
            .SearchEventsAsync(null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>());
        await provider2
            .Received(1)
            .SearchEventsAsync(null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CacheCanonicalSchedule_AndFilterByFromAndToInMemory_WhenQueryOrGenrePresent()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var eventOct = CreateSampleEvent("e-oct", "Bicep Oct", "Drumsheds", new DateOnly(2026, 10, 15));
        var eventNov = CreateSampleEvent("e-nov", "Bicep Nov", "Brixton Academy", new DateOnly(2026, 11, 20));
        var eventDec = CreateSampleEvent("e-dec", "Bicep Dec", "Roundhouse", new DateOnly(2026, 12, 10));

        // When query is present, providers receive null for upstream from and to dates
        provider
            .SearchEventsAsync("Bicep", null, "London", null, null, Arg.Any<CancellationToken>())
            .Returns([eventOct, eventNov, eventDec]);

        var service = CreateService([provider]);

        var from = new DateOnly(2026, 11, 1);
        var to = new DateOnly(2026, 11, 30);

        var result = await service.SearchEventsAsync("Bicep", null, "London", from, to);

        result.ShouldHaveSingleItem().Id.ShouldBe("e-nov");

        // Subsequent query with different date range for same artist hits cache and slices in memory
        var fromDec = new DateOnly(2026, 12, 1);
        var toDec = new DateOnly(2026, 12, 31);

        var resultDec = await service.SearchEventsAsync("Bicep", null, "London", fromDec, toDec);

        resultDec.ShouldHaveSingleItem().Id.ShouldBe("e-dec");

        // Upstream provider should only have been queried ONCE
        await provider.Received(1).SearchEventsAsync("Bicep", null, "London", null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ExcludeEventsWithNullDate_WhenFilteringByDate()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var eventWithDate = CreateSampleEvent("e-date", "Bicep Confirmed", "Drumsheds", new DateOnly(2026, 11, 15));
        var eventTba = CreateSampleEvent("e-tba", "Bicep TBA", "Drumsheds", null, explicitDate: true);

        provider
            .SearchEventsAsync("Bicep", null, "London", null, null, Arg.Any<CancellationToken>())
            .Returns([eventWithDate, eventTba]);

        var service = CreateService([provider]);

        var from = new DateOnly(2026, 11, 1);
        var to = new DateOnly(2026, 11, 30);

        var result = await service.SearchEventsAsync("Bicep", null, "London", from, to);

        result.ShouldHaveSingleItem().Id.ShouldBe("e-date");
    }

    [Fact]
    public async Task Should_PassDatesUpstream_AndUseDateScopedCacheKey_WhenQueryAndGenreAbsent()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 7);

        var dateScopedEvent = CreateSampleEvent("e-ds", "London Club Night", "fabric", new DateOnly(2026, 10, 3));

        provider
            .SearchEventsAsync(null, null, "London", from, to, Arg.Any<CancellationToken>())
            .Returns([dateScopedEvent]);

        var service = CreateService([provider]);

        var result1 = await service.SearchEventsAsync(null, null, "London", from, to);
        var result2 = await service.SearchEventsAsync(null, null, "London", from, to);

        result1.ShouldHaveSingleItem().Id.ShouldBe("e-ds");
        result2.ShouldHaveSingleItem().Id.ShouldBe("e-ds");

        // Provider queried upstream with bounds, cached by date range
        await provider.Received(1).SearchEventsAsync(null, null, "London", from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_AllowOpenEndedDateFilter_WhenQueryIsPresent()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var pastEvent = CreateSampleEvent("e-1", "Bicep Early", "Drumsheds", new DateOnly(2026, 10, 1));
        var futureEvent = CreateSampleEvent("e-2", "Bicep Later", "Brixton", new DateOnly(2026, 11, 15));

        provider
            .SearchEventsAsync("Bicep", null, "London", null, null, Arg.Any<CancellationToken>())
            .Returns([pastEvent, futureEvent]);

        var service = CreateService([provider]);

        var from = new DateOnly(2026, 11, 1);

        var result = await service.SearchEventsAsync("Bicep", null, "London", from: from);

        result.ShouldHaveSingleItem().Id.ShouldBe("e-2");
    }
}
