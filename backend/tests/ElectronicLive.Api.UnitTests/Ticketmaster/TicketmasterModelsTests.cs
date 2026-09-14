using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.UnitTests;

public class TicketmasterModelsTests
{
    [Fact]
    public void Should_MapAllPropertiesCorrectly()
    {
        var ev = new TicketmasterEvent(
            "event-1",
            "Bicep Live",
            "https://ticketmaster.co.uk/event1",
            new TicketmasterDates(new TicketmasterStart("2026-11-26", "18:00:00"), new TicketmasterStatus("onsale")),
            new TicketmasterEventEmbedded([new TicketmasterVenue("Royal Albert Hall")])
        );

        var result = ev.ToEventResponse();

        result.Id.ShouldBe("event-1");
        result.Name.ShouldBe("Bicep Live");
        result.VenueName.ShouldBe("Royal Albert Hall");
        result.Date.ShouldBe(new DateOnly(2026, 11, 26));
        result.Time.ShouldBe(new TimeOnly(18, 0, 0));
        result.TicketUrl.ShouldBe("https://ticketmaster.co.uk/event1");
        result.Status.ShouldBe(EventStatus.OnSale);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_FallbackToUnknownVenue_WhenVenuesEmptyOrNull(bool includeEmptyList)
    {
        var embedded = includeEmptyList ? new TicketmasterEventEmbedded([]) : null;
        var ev = new TicketmasterEvent("id", "name", null, null, embedded);

        var result = ev.ToEventResponse();

        result.VenueName.ShouldBe("Unknown Venue");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_FallbackToUnknownVenue_WhenVenueNameIsEmptyOrWhitespace(string? venueName)
    {
        var embedded = new TicketmasterEventEmbedded([new TicketmasterVenue(venueName)]);
        var ev = new TicketmasterEvent("id", "name", null, null, embedded);

        var result = ev.ToEventResponse();

        result.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public void Should_HandleNullFieldsGracefully()
    {
        var ev = new TicketmasterEvent(null, null, null, null, null);

        var result = ev.ToEventResponse();

        result.Id.ShouldBe(string.Empty);
        result.Name.ShouldBe(string.Empty);
        result.VenueName.ShouldBe("Unknown Venue");
        result.Date.ShouldBeNull();
        result.Time.ShouldBeNull();
        result.TicketUrl.ShouldBeNull();
        result.Status.ShouldBe(EventStatus.Unknown);
    }

    [Theory]
    [InlineData("onsale", EventStatus.OnSale)]
    [InlineData("offsale", EventStatus.SoldOut)]
    [InlineData("canceled", EventStatus.Cancelled)]
    [InlineData("cancelled", EventStatus.Cancelled)]
    [InlineData("postponed", EventStatus.Postponed)]
    [InlineData("rescheduled", EventStatus.Postponed)]
    [InlineData("something_else", EventStatus.Unknown)]
    [InlineData(null, EventStatus.Unknown)]
    public void Should_MapStatusCorrectly(string? statusCode, EventStatus expectedStatus)
    {
        var ev = new TicketmasterEvent(
            "id",
            "name",
            null,
            new TicketmasterDates(null, new TicketmasterStatus(statusCode)),
            null
        );

        var result = ev.ToEventResponse();

        result.Status.ShouldBe(expectedStatus);
    }
}
