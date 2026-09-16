using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public static class EventServicesExtensions
{
    public static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventDeduplicator, EventDeduplicator>();
        services.AddTransient<EventAggregatorService>();
        services.AddTransient<IEventAggregatorService>(sp => new CachedEventAggregatorService(
            sp.GetRequiredService<EventAggregatorService>(),
            sp.GetRequiredService<HybridCache>(),
            sp.GetRequiredService<ILogger<CachedEventAggregatorService>>()
        ));

        return services;
    }
}
