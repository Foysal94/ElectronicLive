using ElectronicLive.Api.Configuration;

namespace ElectronicLive.Api.UnitTests.Configuration;

public class ResidentAdvisorOptionsTests
{
    [Fact]
    public void Should_HaveDefaultBaseUrlWithTrailingSlash()
    {
        var options = new ResidentAdvisorOptions();

        options.BaseUrl.ShouldBe("https://ra.co/");
    }

    [Fact]
    public void Should_EnsureTrailingSlash_WhenBaseUrlLacksTrailingSlash()
    {
        var options = new ResidentAdvisorOptions { BaseUrl = "https://ra.co" };

        options.BaseUrl.ShouldBe("https://ra.co/");
    }

    [Fact]
    public void Should_PreserveTrailingSlash_WhenBaseUrlAlreadyHasTrailingSlash()
    {
        var options = new ResidentAdvisorOptions { BaseUrl = "https://custom-ra.com/" };

        options.BaseUrl.ShouldBe("https://custom-ra.com/");
    }

    [Fact]
    public void Should_HaveExpectedDefaultUserAgent()
    {
        var options = new ResidentAdvisorOptions();

        options.UserAgent.ShouldContain("Mozilla/5.0");
        options.UserAgent.ShouldContain("Chrome");
    }

    [Fact]
    public void Should_HaveExpectedDefaultLimitOf50()
    {
        var options = new ResidentAdvisorOptions();

        options.Limit.ShouldBe(50);
    }

    [Fact]
    public void Should_HaveExpectedSectionName()
    {
        ResidentAdvisorOptions.SectionName.ShouldBe("EventProviders:ResidentAdvisor");
    }
}
