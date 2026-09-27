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
        string? ticketUrl = "https://tickets.com/1"
    ) =>
        new(
            id,
            name,
            venue,
            date ?? new DateOnly(2026, 12, 10),
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
            .SearchEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        provider1.SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>()).Returns([event1]);

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
        provider2.SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>()).Returns([event2]);

        var service = CreateService([provider1, provider2]);

        var result = await service.SearchEventsAsync("Bicep", null, "London");

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(event1.Id);
        result[0].Offers.ShouldNotBeNull().Count.ShouldBe(1);
        result[1].Id.ShouldBe(event2.Id);
        result[1].Offers.ShouldNotBeNull().Count.ShouldBe(1);
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
        tmProvider.SearchEventsAsync("Four Tet", null, "London", Arg.Any<CancellationToken>()).Returns([tmEvent]);

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
        skProvider.SearchEventsAsync("Four Tet", null, "London", Arg.Any<CancellationToken>()).Returns([skEvent]);

        var service = CreateService([tmProvider, skProvider]);

        var result = await service.SearchEventsAsync("Four Tet", null, "London");

        var merged = result.ShouldHaveSingleItem();
        merged.Date.ShouldBe(new DateOnly(2026, 10, 17));
        merged.Time.ShouldBe(new TimeOnly(13, 0));
        merged.Status.ShouldBe(EventStatus.OnSale);
        merged.Provider.ShouldBe(EventProvider.Ticketmaster);
        merged.TicketUrl.ShouldBe("https://ticketmaster.com/1");

        var offers = merged.Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(2);
        offers.ShouldContain(o => o.Provider == EventProvider.Ticketmaster && o.Status == EventStatus.OnSale);
        offers.ShouldContain(o => o.Provider == EventProvider.Skiddle && o.Status == EventStatus.SoldOut);
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
        var eventTba = CreateSampleEvent("4", "Event TBA Date", "Venue 4", null, null);

        provider
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
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
    public async Task Should_ServeSubsequentRequests_FromCache_WithoutRequeryingProviders()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var sampleEvent = CreateSampleEvent("1", "Bicep Live", "Drumsheds");
        provider.SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>()).Returns([sampleEvent]);

        var service = CreateService([provider]);

        var result1 = await service.SearchEventsAsync("Bicep", null, "London");
        var result2 = await service.SearchEventsAsync("bicep", null, "LONDON");

        result1.Count.ShouldBe(1);
        result2.Count.ShouldBe(1);
        await provider
            .Received(1)
            .SearchEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_IsolateProviderFailure_WhenOneProviderThrows()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.Provider.Returns(EventProvider.Ticketmaster);
        failingProvider
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
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
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
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
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
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
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
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
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Upstream exploded"));

        var service = CreateService([failingProvider]);

        var exception = await Should.ThrowAsync<AllProvidersUnavailableException>(() =>
            service.SearchEventsAsync("Bicep", null, "London")
        );
        exception.Query.ShouldBe("Bicep");
        exception.ProviderCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_PropagateOperationCanceledException_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = Substitute.For<IEventProvider>();
        provider
            .SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var service = CreateService([provider]);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.SearchEventsAsync("Bicep", null, "London", cts.Token)
        );
    }

    [Fact]
    public async Task Should_ForwardGenreParameterToAllProvidersConcurrently()
    {
        var provider1 = Substitute.For<IEventProvider>();
        provider1.Provider.Returns(EventProvider.Ticketmaster);
        provider1.SearchEventsAsync(null, "techno", "London", Arg.Any<CancellationToken>()).Returns([]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.Provider.Returns(EventProvider.ResidentAdvisor);
        provider2.SearchEventsAsync(null, "techno", "London", Arg.Any<CancellationToken>()).Returns([]);

        var service = CreateService([provider1, provider2]);

        var result = await service.SearchEventsAsync(null, "techno", "London");

        result.ShouldBeEmpty();
        await provider1.Received(1).SearchEventsAsync(null, "techno", "London", Arg.Any<CancellationToken>());
        await provider2.Received(1).SearchEventsAsync(null, "techno", "London", Arg.Any<CancellationToken>());
    }
}
