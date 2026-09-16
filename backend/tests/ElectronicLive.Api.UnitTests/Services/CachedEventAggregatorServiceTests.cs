using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Services;

public class CachedEventAggregatorServiceTests
{
    private readonly IEventAggregatorService _inner = Substitute.For<IEventAggregatorService>();
    private readonly ILogger<CachedEventAggregatorService> _logger = Substitute.For<
        ILogger<CachedEventAggregatorService>
    >();
    private readonly HybridCache _cache;

    public CachedEventAggregatorServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();
    }

    private CachedEventAggregatorService CreateService() => new(_inner, _cache, _logger);

    private static EventResponse CreateSampleEvent(string id = "e-1", string title = "Bicep Live") =>
        new(
            id,
            title,
            "Drumsheds",
            new DateOnly(2026, 12, 10),
            new TimeOnly(21, 0),
            "https://tickets.com/1",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnEmptyList_WhenQueryIsWhitespace(string? query)
    {
        var service = CreateService();

        var result = await service.SearchEventsAsync(query!);

        result.ShouldBeEmpty();
        await _inner
            .DidNotReceive()
            .SearchEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CallInnerService_OnCacheMiss_AndReturnResults()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        _inner.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(sampleEvents);

        var result = await service.SearchEventsAsync("Bicep", "London");

        result.ShouldBe(sampleEvents);
        await _inner.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ServeSubsequentRequests_FromCache_WithoutCallingInnerService()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        _inner.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(sampleEvents);

        var result1 = await service.SearchEventsAsync("Bicep", "London");
        var result2 = await service.SearchEventsAsync("Bicep", "London");

        result1.ShouldBe(sampleEvents);
        result2.ShouldBe(sampleEvents);
        await _inner.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NormalizeCacheKeys_AcrossCasingAndWhitespace()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        _inner
            .SearchEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sampleEvents);

        var result1 = await service.SearchEventsAsync("  Bicep  ", " London ");
        var result2 = await service.SearchEventsAsync("bicep", "london");
        var result3 = await service.SearchEventsAsync("BICEP", "LONDON");

        result1.ShouldBe(sampleEvents);
        result2.ShouldBe(sampleEvents);
        result3.ShouldBe(sampleEvents);
        await _inner.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ForwardSanitizedArguments_ToInnerService()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        _inner.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(sampleEvents);

        var result = await service.SearchEventsAsync("  Bicep  ", "   ");

        result.ShouldBe(sampleEvents);
        await _inner.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCache_WhenInnerServiceThrowsException()
    {
        var service = CreateService();
        _inner
            .SearchEventsAsync("Overmono", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AllProvidersUnavailableException("Overmono", 3));

        await Should.ThrowAsync<AllProvidersUnavailableException>(() =>
            service.SearchEventsAsync("Overmono", "London")
        );

        // Next call should retry and hit the inner service again rather than serving a cached failure
        var successfulEvents = new List<EventResponse> { CreateSampleEvent("e-2", "Overmono Live") };
        _inner.SearchEventsAsync("Overmono", "London", Arg.Any<CancellationToken>()).Returns(successfulEvents);

        var retryResult = await service.SearchEventsAsync("Overmono", "London");
        retryResult.ShouldBe(successfulEvents);
        await _inner.Received(2).SearchEventsAsync("Overmono", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken_ToInnerService()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        _inner.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([CreateSampleEvent()]);

        await service.SearchEventsAsync("Bicep", "London", cts.Token);

        await _inner
            .Received(1)
            .SearchEventsAsync("Bicep", "London", Arg.Is<CancellationToken>(ct => ct.CanBeCanceled));
    }

    [Fact]
    public async Task Should_ThrowOperationCanceledException_WhenCallerCancels()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.SearchEventsAsync("Bicep", "London", cts.Token)
        );

        await _inner
            .DidNotReceive()
            .SearchEventsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
