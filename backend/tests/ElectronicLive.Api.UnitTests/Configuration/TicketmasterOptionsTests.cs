using ElectronicLive.Api.Configuration;

namespace ElectronicLive.Api.UnitTests;

public class TicketmasterOptionsTests
{
    [Fact]
    public void Should_HaveDefaultBaseUrlWithTrailingSlash()
    {
        var options = new TicketmasterOptions();

        options.BaseUrl.ShouldBe("https://app.ticketmaster.com/discovery/v2/");
    }

    [Fact]
    public void Should_EnsureTrailingSlash_WhenBaseUrlLacksTrailingSlash()
    {
        var options = new TicketmasterOptions { BaseUrl = "https://app.ticketmaster.com/discovery/v2" };

        options.BaseUrl.ShouldBe("https://app.ticketmaster.com/discovery/v2/");
    }

    [Fact]
    public void Should_PreserveTrailingSlash_WhenBaseUrlAlreadyHasTrailingSlash()
    {
        var options = new TicketmasterOptions { BaseUrl = "https://custom-url.com/api/" };

        options.BaseUrl.ShouldBe("https://custom-url.com/api/");
    }

    [Fact]
    public void Should_HaveExpectedSectionName()
    {
        TicketmasterOptions.SectionName.ShouldBe("EventProviders:Ticketmaster");
    }
}
