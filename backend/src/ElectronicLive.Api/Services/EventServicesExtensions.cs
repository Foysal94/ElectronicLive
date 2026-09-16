using Microsoft.Extensions.Caching.Hybrid;

namespace ElectronicLive.Api.Services;

public static class EventServicesExtensions
{
    public static IServiceCollection AddEventServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventDeduplicator, EventDeduplicator>();
        services.AddTransient<IEventAggregatorService, EventAggregatorService>();

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
