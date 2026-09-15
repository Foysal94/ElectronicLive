using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.UnitTests.ResidentAdvisor;

public class ResidentAdvisorModelsTests
{
    [Fact]
    public void Should_MapAllPropertiesCorrectly()
    {
        var item = new RaSearchItem(
            "ra-1",
            "Bicep Live",
            "UPCOMINGEVENT",
            "/events/ra-1",
            "2026-11-26T20:00:00.000",
            "Fabric",
            "London",
            "United Kingdom"
        );

        var result = item.ToEventResponse();

        result.Id.ShouldBe("ra-1");
        result.Name.ShouldBe("Bicep Live");
        result.VenueName.ShouldBe("Fabric");
        result.Date.ShouldBe(new DateOnly(2026, 11, 26));
        result.Time.ShouldBe(new TimeOnly(20, 0, 0));
        result.TicketUrl.ShouldBe("https://ra.co/events/ra-1");
        result.Status.ShouldBe(EventStatus.Unknown);
        result.Provider.ShouldBe(EventProvider.ResidentAdvisor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_FallbackToUnknownVenue_WhenClubNameNullOrWhitespace(string? clubName)
    {
        var item = new RaSearchItem("ra-1", "Gig", "UPCOMINGEVENT", null, null, clubName, "London", "UK");

        var result = item.ToEventResponse();

        result.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public void Should_HandleNullFieldsGracefully()
    {
        var item = new RaSearchItem(null, null, null, null, null, null, null, null);

        var result = item.ToEventResponse();

        result.Id.ShouldBe(string.Empty);
        result.Name.ShouldBe(string.Empty);
        result.VenueName.ShouldBe("Unknown Venue");
        result.Date.ShouldBeNull();
        result.Time.ShouldBeNull();
        result.TicketUrl.ShouldBeNull();
        result.Status.ShouldBe(EventStatus.Unknown);
        result.Provider.ShouldBe(EventProvider.ResidentAdvisor);
    }

    [Theory]
    [InlineData("Four Tet (SOLD OUT)", EventStatus.SoldOut)]
    [InlineData("Bicep (sold out)", EventStatus.SoldOut)]
    [InlineData("Floating Points - Cancelled", EventStatus.Cancelled)]
    [InlineData("Overmono (Canceled)", EventStatus.Cancelled)]
    [InlineData("Bonobo (Postponed)", EventStatus.Postponed)]
    [InlineData("Barry Can't Swim Live", EventStatus.Unknown)]
    public void Should_DetectStatusFromTitle(string title, EventStatus expected)
    {
        var item = new RaSearchItem("1", title, "UPCOMINGEVENT", null, null, "Venue", "London", "UK");

        var result = item.ToEventResponse();

        result.Status.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/events/123", "https://ra.co/events/123")]
    [InlineData("events/123", "https://ra.co/events/123")]
    [InlineData("https://ra.co/events/123", "https://ra.co/events/123")]
    [InlineData("http://ra.co/events/123", "http://ra.co/events/123")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void Should_FormatTicketUrlCorrectly(string? contentUrl, string? expected)
    {
        var item = new RaSearchItem("1", "Event", "UPCOMINGEVENT", contentUrl, null, "Venue", "London", "UK");

        var result = item.ToEventResponse();

        result.TicketUrl.ShouldBe(expected);
    }

    [Fact]
    public void Should_HandleDateWithoutTime_Correctly()
    {
        var item = new RaSearchItem("1", "Event", "UPCOMINGEVENT", null, "2026-11-20", "Venue", "London", "UK");

        var result = item.ToEventResponse();

        result.Date.ShouldBe(new DateOnly(2026, 11, 20));
        result.Time.ShouldBeNull();
    }

    [Fact]
    public void Should_IgnoreMidnightTime_WhenTimestampTimeIsZero()
    {
        var item = new RaSearchItem(
            "1",
            "Event",
            "UPCOMINGEVENT",
            null,
            "2026-11-20T00:00:00.000",
            "Venue",
            "London",
            "UK"
        );

        var result = item.ToEventResponse();

        result.Date.ShouldBe(new DateOnly(2026, 11, 20));
        result.Time.ShouldBeNull();
    }
}
