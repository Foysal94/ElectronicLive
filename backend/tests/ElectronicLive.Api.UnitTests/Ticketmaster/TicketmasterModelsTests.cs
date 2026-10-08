using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.UnitTests.Ticketmaster;

public class TicketmasterModelsTests
{
    [Fact]
    public void Should_MapAllPropertiesCorrectly()
    {
        var ev = new TicketmasterEvent(
            Id: "event-1",
            Name: "Bicep Live",
            Url: "https://ticketmaster.co.uk/event1",
            Dates: new TicketmasterDates(
                new TicketmasterStart(LocalDate: "2026-11-26", LocalTime: "18:00:00"),
                new TicketmasterStatus("onsale")
            ),
            Embedded: new TicketmasterEventEmbedded([new TicketmasterVenue("Royal Albert Hall")])
        );

        var result = ev.ToEventResponse();

        result.Id.ShouldBe("event-1");
        result.Name.ShouldBe("Bicep Live");
        result.VenueName.ShouldBe("Royal Albert Hall");
        result.Date.ShouldBe(new DateOnly(2026, 11, 26));
        result.Time.ShouldBe(new TimeOnly(18, 0, 0));
        result.TicketUrl.ShouldBe("https://ticketmaster.co.uk/event1");
        result.Status.ShouldBe(EventStatus.OnSale);
        result.Provider.ShouldBe(EventProvider.Ticketmaster);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_FallbackToUnknownVenue_WhenVenuesEmptyOrNull(bool includeEmptyList)
    {
        var embedded = includeEmptyList ? new TicketmasterEventEmbedded([]) : null;
        var ev = new TicketmasterEvent(Id: "id", Name: "name", Url: null, Dates: null, Embedded: embedded);

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
        var ev = new TicketmasterEvent(Id: "id", Name: "name", Url: null, Dates: null, Embedded: embedded);

        var result = ev.ToEventResponse();

        result.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public void Should_HandleNullFieldsGracefully()
    {
        var ev = new TicketmasterEvent(Id: null, Name: null, Url: null, Dates: null, Embedded: null);

        var result = ev.ToEventResponse();

        result.Id.ShouldBe(string.Empty);
        result.Name.ShouldBe(string.Empty);
        result.VenueName.ShouldBe("Unknown Venue");
        result.Date.ShouldBeNull();
        result.Time.ShouldBeNull();
        result.TicketUrl.ShouldBeNull();
        result.Status.ShouldBe(EventStatus.Unknown);
        result.Provider.ShouldBe(EventProvider.Ticketmaster);
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
            Id: "id",
            Name: "name",
            Url: null,
            Dates: new TicketmasterDates(Start: null, Status: new TicketmasterStatus(statusCode)),
            Embedded: null
        );

        var result = ev.ToEventResponse();

        result.Status.ShouldBe(expectedStatus);
    }
}
