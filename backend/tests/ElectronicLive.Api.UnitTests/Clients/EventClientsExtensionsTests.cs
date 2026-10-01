using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLive.Api.UnitTests.Clients;

public class EventClientsExtensionsTests
{
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Ticketmaster:BaseUrl"] = "https://app.ticketmaster.com/discovery/v2/",
                ["Ticketmaster:ApiKey"] = "test-ticketmaster-key",
                ["Skiddle:BaseUrl"] = "https://www.skiddle.com/api/v1/",
                ["Skiddle:ApiKey"] = "test-skiddle-key",
                ["ResidentAdvisor:BaseUrl"] = "https://ra.co/graphql",
                ["ResidentAdvisor:UserAgent"] = "ElectronicLiveTest/1.0",
            }
        )
        .Build();

    [Fact]
    public void Should_RegisterAllEventProviders_WhenAddEventClientsCalled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventClients(_configuration);

        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<IEventProvider>().ToList();

        providers.Count.ShouldBe(3);
        providers.ShouldContain(p => p is TicketmasterClient);
        providers.ShouldContain(p => p is SkiddleClient);
        providers.ShouldContain(p => p is ResidentAdvisorClient);
    }

    [Fact]
    public void Should_RegisterArtistVerificationService_ResolvingTicketmasterClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventClients(_configuration);

        var serviceProvider = services.BuildServiceProvider();
        var verificationService = serviceProvider.GetRequiredService<IArtistVerificationService>();

        verificationService.ShouldNotBeNull();
        verificationService.ShouldBeOfType<TicketmasterClient>();
    }

    [Fact]
    public void Should_RegisterConcreteTypedClients()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventClients(_configuration);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<TicketmasterClient>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<SkiddleClient>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ResidentAdvisorClient>().ShouldNotBeNull();
    }
}
