using ElectronicLive.Api.Configuration;

namespace ElectronicLive.Api.UnitTests.Configuration;

public class SkiddleOptionsTests
{
    [Fact]
    public void Should_HaveDefaultBaseUrlWithTrailingSlash()
    {
        var options = new SkiddleOptions();

        options.BaseUrl.ShouldBe("https://www.skiddle.com/api/v1/");
    }

    [Fact]
    public void Should_EnsureTrailingSlash_WhenBaseUrlLacksTrailingSlash()
    {
        var options = new SkiddleOptions { BaseUrl = "https://www.skiddle.com/api/v1" };

        options.BaseUrl.ShouldBe("https://www.skiddle.com/api/v1/");
    }

    [Fact]
    public void Should_PreserveTrailingSlash_WhenBaseUrlAlreadyHasTrailingSlash()
    {
        var options = new SkiddleOptions { BaseUrl = "https://custom-skiddle.com/api/" };

        options.BaseUrl.ShouldBe("https://custom-skiddle.com/api/");
    }

    [Fact]
    public void Should_HaveExpectedSectionName()
    {
        SkiddleOptions.SectionName.ShouldBe("EventProviders:Skiddle");
    }
}
