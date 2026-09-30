using ElectronicLive.Api.Background.Scanner;
using ElectronicLive.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.Background.Scanner;

public sealed class ScanningServicesRegistrationTests
{
    [Fact]
    public void Should_RegisterWatchlistScannerService_AndConfigureOptions()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Scanner:BaseUrl"] = "https://test.electroniclive.co.uk",
            ["Scanner:DelayBetweenArtistsMs"] = "150",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var services = new ServiceCollection();
        services.AddWatchlistScanner(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<ScannerOptions>>().Value;
        options.BaseUrl.ShouldBe("https://test.electroniclive.co.uk");
        options.DelayBetweenArtistsMs.ShouldBe(150);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IWatchlistScannerService));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(WatchlistScannerService));
    }
}
