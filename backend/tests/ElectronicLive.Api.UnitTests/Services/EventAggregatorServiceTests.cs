using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventAggregatorServiceTests
{
    private readonly ILogger<EventAggregatorService> _logger = Substitute.For<ILogger<EventAggregatorService>>();
    private readonly IEventDeduplicator _deduplicator = new EventDeduplicator();

    private EventAggregatorService CreateService(
        IEnumerable<IEventProvider> providers,
        ILogger<EventAggregatorService>? logger = null,
        IEventDeduplicator? deduplicator = null
    ) => new(providers, deduplicator ?? _deduplicator, logger ?? _logger);

    [Fact]
    public async Task Should_ReturnEmptyList_WhenNoProvidersRegistered()
    {
        var service = CreateService([]);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_QueryAllProvidersConcurrently_AndAggregateResults()
    {
        var provider1 = Substitute.For<IEventProvider>();
        provider1.Provider.Returns(EventProvider.Ticketmaster);
        var event1 = new EventResponse(
            "p1-1",
            "Bicep Live",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(20, 0),
            "https://p1.com/1",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        provider1.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([event1]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.Provider.Returns(EventProvider.Ticketmaster);
        var event2 = new EventResponse(
            "p2-1",
            "Bicep DJ Set",
            "Venue 2",
            new DateOnly(2026, 11, 22),
            new TimeOnly(22, 0),
            "https://p2.com/1",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        provider2.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([event2]);

        var service = CreateService([provider1, provider2], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(event1.Id);
        var offers0 = result[0].Offers.ShouldNotBeNull();
        offers0.Count.ShouldBe(1);
        result[1].Id.ShouldBe(event2.Id);
        var offers1 = result[1].Offers.ShouldNotBeNull();
        offers1.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_OrderAggregatedEventsByDateThenTime()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var eventLaterDate = new EventResponse(
            "3",
            "Event Later Date",
            "Venue 3",
            new DateOnly(2026, 12, 1),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var eventEarlierTime = new EventResponse(
            "1",
            "Event Earlier Time",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(18, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var eventLaterTime = new EventResponse(
            "2",
            "Event Later Time",
            "Venue 2",
            new DateOnly(2026, 11, 20),
            new TimeOnly(22, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );

        provider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .Returns([eventLaterDate, eventLaterTime, eventEarlierTime]);

        var service = CreateService([provider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe("1");
        result[1].Id.ShouldBe("2");
        result[2].Id.ShouldBe("3");
    }

    [Fact]
    public async Task Should_SortNullDateAndNullTimeEventsToTheEnd()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);

        var eventNullDate = new EventResponse(
            "null-date",
            "TBA Date Event",
            "Venue TBA",
            null,
            null,
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var eventConfirmed = new EventResponse(
            "confirmed",
            "Confirmed Show",
            "Venue Confirmed",
            new DateOnly(2026, 11, 20),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );

        provider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .Returns([eventNullDate, eventConfirmed]);

        var service = CreateService([provider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe("confirmed");
        result[1].Id.ShouldBe("null-date");
    }

    [Fact]
    public async Task Should_IsolateProviderFailure_WhenOneProviderThrows()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.Provider.Returns(EventProvider.Ticketmaster);
        failingProvider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("500 Internal Server Error"));

        var healthyProvider = Substitute.For<IEventProvider>();
        healthyProvider.Provider.Returns(EventProvider.Ticketmaster);
        var healthyEvent = new EventResponse(
            "h-1",
            "Bicep Live",
            "Venue H",
            new DateOnly(2026, 11, 25),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        healthyProvider.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([healthyEvent]);

        var service = CreateService([failingProvider, healthyProvider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(healthyEvent.Id);
        var offers = result[0].Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_IsolateProviderTimeout_WhenCallerHasNotCancelled()
    {
        using var cts = new CancellationTokenSource();

        var timingOutProvider = Substitute.For<IEventProvider>();
        timingOutProvider.Provider.Returns(EventProvider.Ticketmaster);
        timingOutProvider
            .SearchEventsAsync("Bicep", "London", cts.Token)
            .ThrowsAsync(new TaskCanceledException("HttpClient timeout"));

        var healthyProvider = Substitute.For<IEventProvider>();
        healthyProvider.Provider.Returns(EventProvider.Ticketmaster);
        var healthyEvent = new EventResponse(
            "h-1",
            "Bicep Live",
            "Venue H",
            new DateOnly(2026, 11, 25),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        healthyProvider.SearchEventsAsync("Bicep", "London", cts.Token).Returns([healthyEvent]);

        var service = CreateService([timingOutProvider, healthyProvider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London", cts.Token);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(healthyEvent.Id);
        var healthyOffers = result[0].Offers.ShouldNotBeNull();
        healthyOffers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenAllProvidersFail()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.Provider.Returns(EventProvider.Ticketmaster);
        failingProvider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Upstream exploded"));

        var service = CreateService([failingProvider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_HandleProviderReturningNullListGracefully()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        provider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EventResponse>)null!);

        var service = CreateService([provider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_PropagateOperationCanceledException_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = Substitute.For<IEventProvider>();
        provider.SearchEventsAsync("Bicep", "London", cts.Token).ThrowsAsync(new OperationCanceledException(cts.Token));

        var service = CreateService([provider], _logger);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.SearchEventsAsync("Bicep", "London", cts.Token)
        );
    }

    [Fact]
    public async Task Should_PassCancellationTokenToAllProviders()
    {
        using var cts = new CancellationTokenSource();

        var provider1 = Substitute.For<IEventProvider>();
        provider1.SearchEventsAsync("Bicep", "London", cts.Token).Returns([]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.SearchEventsAsync("Bicep", "London", cts.Token).Returns([]);

        var service = CreateService([provider1, provider2], _logger);

        await service.SearchEventsAsync("Bicep", "London", cts.Token);

        await provider1.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
        await provider2.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
    }

    [Fact]
    public async Task Should_DelegateToDeduplicator_WhenAggregatingResults()
    {
        var mockDeduplicator = Substitute.For<IEventDeduplicator>();
        var provider = Substitute.For<IEventProvider>();
        provider.Provider.Returns(EventProvider.Ticketmaster);
        var rawEvent = new EventResponse(
            "1",
            "Raw Name",
            "Venue",
            new DateOnly(2026, 11, 20),
            null,
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        provider.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([rawEvent]);

        var deduplicatedEvent = rawEvent with { Name = "Deduplicated Name" };
        mockDeduplicator.Deduplicate(Arg.Any<IEnumerable<EventResponse>>()).Returns([deduplicatedEvent]);

        var service = CreateService([provider], deduplicator: mockDeduplicator);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldHaveSingleItem().Name.ShouldBe("Deduplicated Name");
        mockDeduplicator.Received(1).Deduplicate(Arg.Is<IEnumerable<EventResponse>>(e => e.Contains(rawEvent)));
    }
}
