using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests;

public class EventEndpointsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchEvents_ReturnsBadRequest_WhenArtistIsInvalid(string? artist)
    {
        var service = new FakeTicketmasterService();

        var result = await EventEndpoints.SearchEvents(artist, service);

        var badRequest = result.Result.ShouldBeOfType<BadRequest<string>>();
        badRequest.Value.ShouldBe("Artist query parameter is required.");
    }

    [Fact]
    public async Task SearchEvents_ReturnsOk_WithEvents_WhenArtistIsValid()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                "ev-1",
                "Bicep Live",
                "Royal Albert Hall",
                "2026-11-26",
                "18:00:00",
                "https://ticketmaster.co.uk/event1",
                EventStatus.OnSale
            ),
        };
        var service = new FakeTicketmasterService { EventsToReturn = expectedEvents };

        var result = await EventEndpoints.SearchEvents("Bicep", service);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
        service.LastCityPassed.ShouldBe("London");
    }

    [Fact]
    public async Task SearchEvents_PassesCustomCity_WhenSpecified()
    {
        var service = new FakeTicketmasterService();

        await EventEndpoints.SearchEvents("Bicep", service, "Manchester");

        service.LastCityPassed.ShouldBe("Manchester");
    }

    private sealed class FakeTicketmasterService : ITicketmasterService
    {
        public IReadOnlyList<EventResponse> EventsToReturn { get; set; } = [];
        public string? LastCityPassed { get; private set; }

        public Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
            string artistName,
            string? city = "London",
            CancellationToken cancellationToken = default
        )
        {
            LastCityPassed = city;
            return Task.FromResult(EventsToReturn);
        }
    }
}
