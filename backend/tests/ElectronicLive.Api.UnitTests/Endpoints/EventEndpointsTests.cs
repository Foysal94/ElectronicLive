using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Endpoints;

public class EventEndpointsTests
{
    private readonly IEventSearchService _searchService = Substitute.For<IEventSearchService>();

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "")]
    [InlineData("", null)]
    public async Task Should_ReturnValidationProblem_WhenBothQueryAndGenreAreEmpty(string? query, string? genre)
    {
        var result = await EventEndpoints.SearchEvents(query, genre, _searchService);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("query");
        validationProblem
            .ProblemDetails.Errors["query"]
            .ShouldContain("At least one of query or genre parameter is required.");
        await _searchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default, default);
    }

    [Theory]
    [InlineData("polka")]
    [InlineData("rock")]
    [InlineData("hiphop")]
    [InlineData("123")]
    public async Task Should_ReturnValidationProblem_WhenGenreIsInvalid(string invalidGenre)
    {
        var result = await EventEndpoints.SearchEvents(null, invalidGenre, _searchService);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("genre");
        validationProblem.ProblemDetails.Errors["genre"][0].ShouldContain($"Invalid genre '{invalidGenre}'.");
        await _searchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default, default);
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
        _searchService
            .SearchEventsAsync("fabric", null, "London", Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("fabric", null, _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenOnlyGenreIsProvided()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                "ev-techno",
                "KNTXT London",
                "FOLD",
                new DateOnly(2026, 12, 5),
                new TimeOnly(23, 0, 0),
                "https://ra.co/events/2001",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };
        _searchService
            .SearchEventsAsync(null, "techno", "London", Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents(null, "techno", _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenBothQueryAndGenreAreProvided()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                "ev-charlotte",
                "Charlotte de Witte",
                "FOLD",
                new DateOnly(2026, 12, 5),
                new TimeOnly(23, 0, 0),
                "https://ra.co/events/2001",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };
        _searchService
            .SearchEventsAsync("Charlotte", "techno", "London", Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("Charlotte", "techno", _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _searchService.SearchEventsAsync("fabric", null, "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", null, _searchService, city: "Manchester");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _searchService.Received(1).SearchEventsAsync("fabric", null, "Manchester", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultCityToLondon_WhenCityNullOrWhitespace(string? city)
    {
        _searchService.SearchEventsAsync("fabric", null, "London", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", null, _searchService, city: city);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _searchService.Received(1).SearchEventsAsync("fabric", null, "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrimQueryAndCityAndGenre_WhenWhitespacePresent()
    {
        _searchService.SearchEventsAsync("fabric", "techno", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        await EventEndpoints.SearchEvents("  fabric  ", "  Techno  ", _searchService, city: "  Manchester  ");

        await _searchService
            .Received(1)
            .SearchEventsAsync("fabric", "techno", "Manchester", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _searchService.SearchEventsAsync("fabric", null, "London", cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("fabric", null, _searchService, cancellationToken: cts.Token);

        await _searchService.Received(1).SearchEventsAsync("fabric", null, "London", cts.Token);
    }

    [Fact]
    public async Task Should_Return502BadGateway_WhenAllProvidersFail()
    {
        _searchService
            .SearchEventsAsync("fabric", null, "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AllProvidersUnavailableException("fabric", 3));

        var result = await EventEndpoints.SearchEvents("fabric", null, _searchService);

        var problemResult = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problemResult.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        problemResult.ProblemDetails.Title.ShouldBe("Upstream Providers Unavailable");
        problemResult.ProblemDetails.Detail.ShouldBe("All external event providers failed to respond.");
    }
}
