using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.UnitTests.Skiddle;

public class SkiddleModelsTests
{
    [Fact]
    public void Should_MapAllPropertiesCorrectly()
    {
        var ev = new SkiddleEvent(
            "sk-1",
            "Bicep Live",
            "2026-11-26",
            new SkiddleOpeningTimes("19:00"),
            "https://www.skiddle.com/whats-on/London/Drumsheds/event1/",
            false,
            true,
            new SkiddleVenue("Drumsheds", "London")
        );

        var result = ev.ToEventResponse();

        result.Id.ShouldBe("sk-1");
        result.Name.ShouldBe("Bicep Live");
        result.VenueName.ShouldBe("Drumsheds");
        result.Date.ShouldBe(new DateOnly(2026, 11, 26));
        result.Time.ShouldBe(new TimeOnly(19, 0, 0));
        result.TicketUrl.ShouldBe("https://www.skiddle.com/whats-on/London/Drumsheds/event1/");
        result.Status.ShouldBe(EventStatus.OnSale);
        result.Provider.ShouldBe(EventProvider.Skiddle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_FallbackToUnknownVenue_WhenVenueNullOrWhitespace(string? venueName)
    {
        var venue = venueName == null ? null : new SkiddleVenue(venueName, "London");
        var ev = new SkiddleEvent("id", "name", null, null, null, false, true, venue);

        var result = ev.ToEventResponse();

        result.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public void Should_HandleNullFieldsGracefully()
    {
        var ev = new SkiddleEvent(null, null, null, null, null, null, null, null);

        var result = ev.ToEventResponse();

        result.Id.ShouldBe(string.Empty);
        result.Name.ShouldBe(string.Empty);
        result.VenueName.ShouldBe("Unknown Venue");
        result.Date.ShouldBeNull();
        result.Time.ShouldBeNull();
        result.TicketUrl.ShouldBeNull();
        result.Status.ShouldBe(EventStatus.Unknown);
        result.Provider.ShouldBe(EventProvider.Skiddle);
    }

    [Theory]
    [InlineData(true, true, EventStatus.Cancelled)]
    [InlineData("1", true, EventStatus.Cancelled)]
    [InlineData(false, false, EventStatus.SoldOut)]
    [InlineData("0", "0", EventStatus.SoldOut)]
    [InlineData(false, true, EventStatus.OnSale)]
    [InlineData("0", "1", EventStatus.OnSale)]
    [InlineData(null, null, EventStatus.Unknown)]
    public void Should_MapStatusCorrectly(object? cancelled, object? tickets, EventStatus expectedStatus)
    {
        var ev = new SkiddleEvent("id", "name", null, null, null, cancelled, tickets, null);

        var result = ev.ToEventResponse();

        result.Status.ShouldBe(expectedStatus);
    }
}
