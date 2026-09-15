using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests;

public class EventEndpointsTests
{
    private readonly IEventAggregatorService _aggregatorService = Substitute.For<IEventAggregatorService>();

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    [InlineData("   ", "   ", "   ")]
    public async Task Should_ReturnValidationProblem_WhenQueryAndArtistAreEmpty(
        string? query,
        string? artist,
        string? q
    )
    {
        var result = await EventEndpoints.SearchEvents(query, artist, _aggregatorService, q: q);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("query");
        validationProblem.ProblemDetails.Errors["query"].ShouldContain("Search query parameter is required.");
        await _aggregatorService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default!);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenQueryIsValid()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                "ev-1",
                "fabric Saturdays",
                "fabric",
                new DateOnly(2026, 11, 28),
                new TimeOnly(23, 0, 0),
                "https://fabriclondon.com/event1",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };
        _aggregatorService.SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>()).Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("fabric", null, _aggregatorService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_SupportArtistParameter_ForBackwardCompatibility()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                "ev-2",
                "Bicep Live",
                "Royal Albert Hall",
                new DateOnly(2026, 11, 26),
                new TimeOnly(18, 0, 0),
                "https://ticketmaster.co.uk/event2",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };
        _aggregatorService.SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>()).Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents(null, "Bicep", _aggregatorService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
        await _aggregatorService.Received(1).SearchEventsAsync("Bicep", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SupportQAlias_WhenQueryAndArtistNotProvided()
    {
        _aggregatorService.SearchEventsAsync("Drumsheds", "London", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents(null, null, _aggregatorService, q: "Drumsheds");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _aggregatorService.Received(1).SearchEventsAsync("Drumsheds", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PrioritizeQueryOverArtist_WhenBothProvided()
    {
        _aggregatorService.SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", "Bicep", _aggregatorService);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _aggregatorService.SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", null, _aggregatorService, city: "Manchester");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultCityToLondon_WhenCityNullOrWhitespace(string? city)
    {
        _aggregatorService.SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", null, _aggregatorService, city: city);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrimSearchTermAndCity_WhenWhitespacePresent()
    {
        _aggregatorService.SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        await EventEndpoints.SearchEvents("  fabric  ", null, _aggregatorService, city: "  Manchester  ");

        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _aggregatorService.SearchEventsAsync("fabric", "London", cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("fabric", null, _aggregatorService, cancellationToken: cts.Token);

        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "London", cts.Token);
    }
}
