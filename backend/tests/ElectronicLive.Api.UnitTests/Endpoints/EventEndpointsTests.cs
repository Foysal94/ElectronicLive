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
    private readonly IEventAggregatorService _aggregatorService = Substitute.For<IEventAggregatorService>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnValidationProblem_WhenQueryIsEmpty(string? query)
    {
        var result = await EventEndpoints.SearchEvents(query, _aggregatorService);

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

        var result = await EventEndpoints.SearchEvents("fabric", _aggregatorService);

        var okResult = result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        okResult.Value.ShouldBe(expectedEvents);
    }

    [Fact]
    public async Task Should_PassCustomCity_WhenSpecified()
    {
        _aggregatorService.SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        var result = await EventEndpoints.SearchEvents("fabric", _aggregatorService, city: "Manchester");

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

        var result = await EventEndpoints.SearchEvents("fabric", _aggregatorService, city: city);

        result.Result.ShouldBeOfType<Ok<IReadOnlyList<EventResponse>>>();
        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrimQueryAndCity_WhenWhitespacePresent()
    {
        _aggregatorService.SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>()).Returns([]);

        await EventEndpoints.SearchEvents("  fabric  ", _aggregatorService, city: "  Manchester  ");

        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "Manchester", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _aggregatorService.SearchEventsAsync("fabric", "London", cts.Token).Returns([]);

        await EventEndpoints.SearchEvents("fabric", _aggregatorService, cancellationToken: cts.Token);

        await _aggregatorService.Received(1).SearchEventsAsync("fabric", "London", cts.Token);
    }

    [Fact]
    public async Task Should_Return502BadGateway_WhenAllProvidersFail()
    {
        _aggregatorService
            .SearchEventsAsync("fabric", "London", Arg.Any<CancellationToken>())
            .ThrowsAsync(new AllProvidersUnavailableException("fabric", 3));

        var result = await EventEndpoints.SearchEvents("fabric", _aggregatorService);

        var problemResult = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problemResult.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        problemResult.ProblemDetails.Title.ShouldBe("Upstream Providers Unavailable");
        problemResult.ProblemDetails.Detail.ShouldBe("All external event providers failed to respond.");
    }
}
