using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventAggregatorServiceTests
{
    private readonly ILogger<EventAggregatorService> _logger = Substitute.For<ILogger<EventAggregatorService>>();

    [Fact]
    public async Task Should_ReturnEmptyList_WhenNoProvidersRegistered()
    {
        var service = new EventAggregatorService([], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_QueryAllProvidersConcurrently_AndAggregateResults()
    {
        var provider1 = Substitute.For<IEventProvider>();
        provider1.ProviderName.Returns("Provider1");
        var event1 = new EventResponse(
            "p1-1",
            "Bicep Live",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(20, 0),
            "https://p1.com/1",
            EventStatus.OnSale,
            "Provider1"
        );
        provider1.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([event1]);

        var provider2 = Substitute.For<IEventProvider>();
        provider2.ProviderName.Returns("Provider2");
        var event2 = new EventResponse(
            "p2-1",
            "Bicep DJ Set",
            "Venue 2",
            new DateOnly(2026, 11, 22),
            new TimeOnly(22, 0),
            "https://p2.com/1",
            EventStatus.OnSale,
            "Provider2"
        );
        provider2.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([event2]);

        var service = new EventAggregatorService([provider1, provider2], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(2);
        result[0].ShouldBe(event1);
        result[1].ShouldBe(event2);
    }

    [Fact]
    public async Task Should_OrderAggregatedEventsByDateThenTime()
    {
        var provider = Substitute.For<IEventProvider>();
        provider.ProviderName.Returns("Provider1");

        var eventLaterDate = new EventResponse(
            "3",
            "Event Later Date",
            "Venue 3",
            new DateOnly(2026, 12, 1),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            "Provider1"
        );
        var eventEarlierTime = new EventResponse(
            "1",
            "Event Earlier Time",
            "Venue 1",
            new DateOnly(2026, 11, 20),
            new TimeOnly(18, 0),
            null,
            EventStatus.OnSale,
            "Provider1"
        );
        var eventLaterTime = new EventResponse(
            "2",
            "Event Later Time",
            "Venue 2",
            new DateOnly(2026, 11, 20),
            new TimeOnly(22, 0),
            null,
            EventStatus.OnSale,
            "Provider1"
        );

        provider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .Returns([eventLaterDate, eventLaterTime, eventEarlierTime]);

        var service = new EventAggregatorService([provider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe("1");
        result[1].Id.ShouldBe("2");
        result[2].Id.ShouldBe("3");
    }

    [Fact]
    public async Task Should_IsolateProviderFailure_WhenOneProviderThrows()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.ProviderName.Returns("FailingProvider");
        failingProvider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("500 Internal Server Error"));

        var healthyProvider = Substitute.For<IEventProvider>();
        healthyProvider.ProviderName.Returns("HealthyProvider");
        var healthyEvent = new EventResponse(
            "h-1",
            "Bicep Live",
            "Venue H",
            new DateOnly(2026, 11, 25),
            new TimeOnly(20, 0),
            null,
            EventStatus.OnSale,
            "HealthyProvider"
        );
        healthyProvider.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([healthyEvent]);

        var service = new EventAggregatorService([failingProvider, healthyProvider], _logger);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.Count.ShouldBe(1);
        result[0].ShouldBe(healthyEvent);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenAllProvidersFail()
    {
        var failingProvider = Substitute.For<IEventProvider>();
        failingProvider.ProviderName.Returns("FailingProvider");
        failingProvider
            .SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Upstream exploded"));

        var service = new EventAggregatorService([failingProvider], _logger);

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

        var service = new EventAggregatorService([provider], _logger);

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

        var service = new EventAggregatorService([provider1, provider2], _logger);

        await service.SearchEventsAsync("Bicep", "London", cts.Token);

        await provider1.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
        await provider2.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
    }
}
