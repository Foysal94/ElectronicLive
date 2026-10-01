using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Clients;

public static class EventClientsExtensions
{
    public static IServiceCollection AddEventClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TicketmasterOptions>(configuration.GetSection(TicketmasterOptions.SectionName));
        services.Configure<SkiddleOptions>(configuration.GetSection(SkiddleOptions.SectionName));
        services.Configure<ResidentAdvisorOptions>(configuration.GetSection(ResidentAdvisorOptions.SectionName));

        services
            .AddHttpClient<TicketmasterClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<TicketmasterOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<SkiddleClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<SkiddleOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<ResidentAdvisorClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<ResidentAdvisorOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                }
            )
            .AddStandardResilienceHandler();

        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<TicketmasterClient>());
        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<SkiddleClient>());
        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<ResidentAdvisorClient>());
        services.AddTransient<IArtistVerificationService>(sp => sp.GetRequiredService<TicketmasterClient>());

        return services;
    }
}
