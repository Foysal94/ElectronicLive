namespace ElectronicLive.Api.Services;

public static class EventServicesExtensions
{
    public static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventDeduplicator, EventDeduplicator>();
        services.AddTransient<IEventAggregatorService, EventAggregatorService>();
        services.AddTransient<IEventCacheService, EventCacheService>();

        return services;
    }
}
