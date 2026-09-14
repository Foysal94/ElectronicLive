using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests;

public class EventEndpointsTests
{
    private readonly ITicketmasterClient _ticketmasterClient = Substitute.For<ITicketmasterClient>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnValidationProblem_WhenArtistIsInvalid(string? artist)
    {
        var result = await EventEndpoints.SearchEvents(artist, _ticketmasterClient);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("artist");
        validationProblem.ProblemDetails.Errors["artist"].ShouldContain("Artist query parameter is required.");
        await _ticketmasterClient.DidNotReceiveWithAnyArgs().SearchEventsAsync(default!);
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
                new DateOnly(2026, 11, 26),
                new TimeOnly(18, 0, 0),
                "https://ticketmaster.co.uk/event1",
                EventStatus.OnSale
            ),
        };
        _ticketmasterClient.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("Bicep", _ticketmasterClient);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _ticketmasterClient.SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("Bicep", _ticketmasterClient, "Manchester");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _ticketmasterClient.Received(1).SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultCityToLondon_WhenCityNullOrWhitespace(string? city)
    {
        _ticketmasterClient.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("Bicep", _ticketmasterClient, city);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _ticketmasterClient.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrimArtistAndCity_WhenWhitespacePresent()
    {
        _ticketmasterClient.SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        await EventEndpoints.SearchEvents("  Bicep  ", _ticketmasterClient, "  Manchester  ");

        await _ticketmasterClient.Received(1).SearchEventsAsync("Bicep", "Manchester", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _ticketmasterClient.SearchEventsAsync("Bicep", "London", cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("Bicep", _ticketmasterClient, cancellationToken: cts.Token);

        await _ticketmasterClient.Received(1).SearchEventsAsync("Bicep", "London", cts.Token);
    }
}
