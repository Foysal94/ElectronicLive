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
        var result = await EventEndpoints.SearchEvents(query: null, invalidGenre, _searchService);

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
                Id: "ev-1",
                Name: "fabric Saturdays",
                VenueName: "fabric",
                Date: new DateOnly(2026, 11, 28),
                Time: new TimeOnly(23, 0, 0),
                TicketUrl: "https://fabriclondon.com/event1",
                Status: EventStatus.OnSale,
                Provider: EventProvider.Ticketmaster
            ),
        };
        _searchService
            .SearchEventsAsync("fabric", genre: null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("fabric", genre: null, _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenOnlyGenreIsProvided()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                Id: "ev-techno",
                Name: "KNTXT London",
                VenueName: "FOLD",
                Date: new DateOnly(2026, 12, 5),
                Time: new TimeOnly(23, 0, 0),
                TicketUrl: "https://ra.co/events/2001",
                Status: EventStatus.OnSale,
                Provider: EventProvider.ResidentAdvisor
            ),
        };
        _searchService
            .SearchEventsAsync(query: null, "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents(query: null, "techno", _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenBothQueryAndGenreAreProvided()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                Id: "ev-charlotte",
                Name: "Charlotte de Witte",
                VenueName: "FOLD",
                Date: new DateOnly(2026, 12, 5),
                Time: new TimeOnly(23, 0, 0),
                TicketUrl: "https://ra.co/events/2001",
                Status: EventStatus.OnSale,
                Provider: EventProvider.ResidentAdvisor
            ),
        };
        _searchService
            .SearchEventsAsync("Charlotte", "techno", "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("Charlotte", "techno", _searchService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenToIsBeforeFrom()
    {
        var from = new DateOnly(2026, 10, 10);
        var to = new DateOnly(2026, 10, 5);

        var result = await EventEndpoints.SearchEvents("Bicep", genre: null, _searchService, from: from, to: to);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("to");
        validationProblem.ProblemDetails.Errors["to"][0].ShouldContain("greater than or equal to");
        await _searchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenDateOnlyQueryMissingOneDateBoundary()
    {
        var from = new DateOnly(2026, 10, 10);

        var result = await EventEndpoints.SearchEvents(query: null, genre: null, _searchService, from: from, to: null);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("date");
        validationProblem
            .ProblemDetails.Errors["date"][0]
            .ShouldContain("Both 'from' and 'to' date parameters are required");
        await _searchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenDateOnlyRangeExceeds7Days()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 9); // 8 days

        var result = await EventEndpoints.SearchEvents(query: null, genre: null, _searchService, from: from, to: to);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("date");
        validationProblem.ProblemDetails.Errors["date"][0].ShouldContain("cannot exceed 7 days");
        await _searchService.DidNotReceiveWithAnyArgs().SearchEventsAsync(default, default);
    }

    [Fact]
    public async Task Should_ReturnOkWithEvents_WhenDateOnlyQueryIsWithin7Days()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 8); // exactly 7 days
        var expectedEvents = new List<EventResponse>();

        _searchService
            .SearchEventsAsync(query: null, genre: null, "London", from, to, Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents(query: null, genre: null, _searchService, from: from, to: to);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_AllowDateRangeExceeding7Days_WhenQueryIsPresent()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 11, 15); // 45 days
        var expectedEvents = new List<EventResponse>();

        _searchService
            .SearchEventsAsync("Bicep", genre: null, "London", from, to, Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var result = await EventEndpoints.SearchEvents("Bicep", genre: null, _searchService, from: from, to: to);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _searchService
            .SearchEventsAsync("fabric", genre: null, "Manchester", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", genre: null, _searchService, city: "Manchester");

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _searchService
            .Received(1)
            .SearchEventsAsync("fabric", genre: null, "Manchester", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultCityToLondon_WhenCityNullOrWhitespace(string? city)
    {
        _searchService
            .SearchEventsAsync("fabric", genre: null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", genre: null, _searchService, city: city);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _searchService
            .Received(1)
            .SearchEventsAsync("fabric", genre: null, "London", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrimQueryAndCityAndGenre_WhenWhitespacePresent()
    {
        _searchService
            .SearchEventsAsync("fabric", "techno", "Manchester", cancellationToken: Arg.Any<CancellationToken>())
            .Returns([]);

        await EventEndpoints.SearchEvents("  fabric  ", "  Techno  ", _searchService, city: "  Manchester  ");

        await _searchService
            .Received(1)
            .SearchEventsAsync("fabric", "techno", "Manchester", cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _searchService.SearchEventsAsync("fabric", genre: null, "London", cancellationToken: cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("fabric", genre: null, _searchService, cancellationToken: cts.Token);

        await _searchService
            .Received(1)
            .SearchEventsAsync("fabric", genre: null, "London", cancellationToken: cts.Token);
    }

    [Fact]
    public async Task Should_Return502BadGateway_WhenAllProvidersFail()
    {
        _searchService
            .SearchEventsAsync("fabric", genre: null, "London", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new AllProvidersUnavailableException("fabric", 3));

        var result = await EventEndpoints.SearchEvents("fabric", genre: null, _searchService);

        var problemResult = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problemResult.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        problemResult.ProblemDetails.Title.ShouldBe("Upstream Providers Unavailable");
        problemResult.ProblemDetails.Detail.ShouldBe("All external event providers failed to respond.");
    }
}
