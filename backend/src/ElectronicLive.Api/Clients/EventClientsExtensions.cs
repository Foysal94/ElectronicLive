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
            .AddHttpClient<ITicketmasterClient, TicketmasterClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<TicketmasterOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IArtistVerificationService, TicketmasterArtistVerificationService>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<TicketmasterOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<ISkiddleClient, SkiddleClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<SkiddleOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IResidentAdvisorClient, ResidentAdvisorClient>(
                (sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<ResidentAdvisorOptions>>().Value;
                    client.BaseAddress = new Uri(options.BaseUrl);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                }
            )
            .AddStandardResilienceHandler();

        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<ITicketmasterClient>());
        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<ISkiddleClient>());
        services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<IResidentAdvisorClient>());

        return services;
    }
}
