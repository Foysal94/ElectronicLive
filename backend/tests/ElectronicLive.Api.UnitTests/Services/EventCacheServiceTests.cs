using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventCacheServiceTests
{
    private readonly ILogger<EventCacheService> _logger = Substitute.For<ILogger<EventCacheService>>();
    private readonly HybridCache _cache;

    public EventCacheServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();
    }

    private EventCacheService CreateService() => new(_cache, _logger);

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
        var factoryCalled = false;

        var result = await service.GetOrAddAsync(
            query!,
            "London",
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult<IReadOnlyList<EventResponse>>([CreateSampleEvent()]);
            }
        );

        result.ShouldBeEmpty();
        factoryCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ExecuteFactory_OnCacheMiss_AndReturnResults()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        var factoryCallCount = 0;

        var result = await service.GetOrAddAsync(
            "Bicep",
            "London",
            _ =>
            {
                factoryCallCount++;
                return Task.FromResult<IReadOnlyList<EventResponse>>(sampleEvents);
            }
        );

        result.ShouldBe(sampleEvents);
        factoryCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_ServeSubsequentRequests_FromCache_WithoutExecutingFactory()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        var factoryCallCount = 0;

        Task<IReadOnlyList<EventResponse>> Factory(CancellationToken _)
        {
            factoryCallCount++;
            return Task.FromResult<IReadOnlyList<EventResponse>>(sampleEvents);
        }

        var result1 = await service.GetOrAddAsync("Bicep", "London", Factory);
        var result2 = await service.GetOrAddAsync("Bicep", "London", Factory);

        result1.ShouldBe(sampleEvents);
        result2.ShouldBe(sampleEvents);
        factoryCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_NormalizeCacheKeys_AcrossCasingAndWhitespace()
    {
        var service = CreateService();
        var sampleEvents = new List<EventResponse> { CreateSampleEvent() };
        var factoryCallCount = 0;

        Task<IReadOnlyList<EventResponse>> Factory(CancellationToken _)
        {
            factoryCallCount++;
            return Task.FromResult<IReadOnlyList<EventResponse>>(sampleEvents);
        }

        var result1 = await service.GetOrAddAsync("  Bicep  ", " London ", Factory);
        var result2 = await service.GetOrAddAsync("bicep", "london", Factory);
        var result3 = await service.GetOrAddAsync("BICEP", "LONDON", Factory);

        result1.ShouldBe(sampleEvents);
        result2.ShouldBe(sampleEvents);
        result3.ShouldBe(sampleEvents);
        factoryCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_NotCache_WhenFactoryThrowsException()
    {
        var service = CreateService();
        var factoryCallCount = 0;

        await Should.ThrowAsync<AllProvidersUnavailableException>(() =>
            service.GetOrAddAsync(
                "Overmono",
                "London",
                _ =>
                {
                    factoryCallCount++;
                    throw new AllProvidersUnavailableException("Overmono", 3);
                }
            )
        );

        factoryCallCount.ShouldBe(1);

        // Next call should retry and invoke factory again
        var successfulEvents = new List<EventResponse> { CreateSampleEvent("e-2", "Overmono Live") };
        var retryResult = await service.GetOrAddAsync(
            "Overmono",
            "London",
            _ =>
            {
                factoryCallCount++;
                return Task.FromResult<IReadOnlyList<EventResponse>>(successfulEvents);
            }
        );

        retryResult.ShouldBe(successfulEvents);
        factoryCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_PropagateCancellationToken_ToFactory()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        var receivedCancellationToken = CancellationToken.None;

        await service.GetOrAddAsync(
            "Bicep",
            "London",
            ct =>
            {
                receivedCancellationToken = ct;
                return Task.FromResult<IReadOnlyList<EventResponse>>([CreateSampleEvent()]);
            },
            cts.Token
        );

        receivedCancellationToken.CanBeCanceled.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ThrowOperationCanceledException_WhenCallerCancels()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.GetOrAddAsync(
                "Bicep",
                "London",
                _ => Task.FromResult<IReadOnlyList<EventResponse>>([CreateSampleEvent()]),
                cts.Token
            )
        );
    }
}
