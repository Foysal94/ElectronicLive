using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests;

public class EventEndpointsTests
{
    private readonly ITicketmasterService _ticketmasterService = Substitute.For<ITicketmasterService>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnBadRequest_WhenArtistIsInvalid(string? artist)
    {
        var result = await EventEndpoints.SearchEvents(artist, _ticketmasterService);

        var badRequest = result.Result.ShouldBeOfType<BadRequest<string>>();
        badRequest.Value.ShouldBe("Artist query parameter is required.");
        await _ticketmasterService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default!);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenArtistIsValid()
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
        _ticketmasterService.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("Bicep", _ticketmasterService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _ticketmasterService.SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("Bicep", _ticketmasterService, "Manchester");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _ticketmasterService.Received(1).SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _ticketmasterService.SearchEventsAsync("Bicep", "London", cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("Bicep", _ticketmasterService, cancellationToken: cts.Token);

        await _ticketmasterService.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
    }
}
