using ElectronicLive.Api.Configuration;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Services;

public static class EventServicesExtensions
{
    public static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddTransient<IEventSearchService, EventSearchService>();
        services.AddTransient<ISubscriptionService, SubscriptionService>();

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

        return services;
    }

    public static IServiceCollection AddEmailDispatching(this IServiceCollection services, IConfiguration configuration)
    {
        var resendSection = configuration.GetSection(ResendOptions.SectionName);
        services.Configure<ResendOptions>(resendSection);

        var resendOptions = resendSection.Get<ResendOptions>() ?? new ResendOptions();
        var apiKey = configuration["Resend:ApiKey"] ?? configuration["Resend__ApiKey"] ?? resendOptions.ApiKey;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            services
                .AddHttpClient<IEmailDispatcher, ResendEmailDispatcher>(
                    (sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<ResendOptions>>().Value;
                        client.BaseAddress = new Uri(options.BaseUrl);
                    }
                )
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddTransient<IEmailDispatcher, LoggingEmailDispatcher>();
        }

        return services;
    }
}
