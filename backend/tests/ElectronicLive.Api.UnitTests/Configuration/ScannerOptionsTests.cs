using ElectronicLive.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace ElectronicLive.Api.UnitTests.Configuration;

public sealed class ScannerOptionsTests
{
    [Fact]
    public void Should_HaveCorrectDefaultValues()
    {
        var options = new ScannerOptions();

        options.BaseUrl.ShouldBe("https://electroniclive.co.uk");
        options.DelayBetweenArtistsMs.ShouldBe(300);
    }

    [Fact]
    public void Should_BindFromConfigurationCorrectly()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Scanner:BaseUrl"] = "https://custom.electroniclive.co.uk",
            ["Scanner:DelayBetweenArtistsMs"] = "500",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var options = configuration.GetSection(ScannerOptions.SectionName).Get<ScannerOptions>();

        options.ShouldNotBeNull();
        options.BaseUrl.ShouldBe("https://custom.electroniclive.co.uk");
        options.DelayBetweenArtistsMs.ShouldBe(500);
    }
}
