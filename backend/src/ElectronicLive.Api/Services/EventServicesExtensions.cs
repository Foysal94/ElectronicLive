using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public static class EventServicesExtensions
{
    public static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventDeduplicator, EventDeduplicator>();
        services.AddTransient<IEventAggregatorService, EventAggregatorService>();

        return services;
    }

    public static IServiceCollection AddEventCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheExpirationMinutes = configuration.GetValue("Cache:ExpirationMinutes", 30);
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes),
            };
        });
        services.AddTransient<IEventCacheService, EventCacheService>();

        return services;
    }
}
