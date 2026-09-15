using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/search", SearchEvents);

        return group;
    }

    internal static async Task<
        Results<Ok<IReadOnlyList<EventResponse>>, ValidationProblem, ProblemHttpResult>
    > SearchEvents(
        string? query,
        IEventAggregatorService eventAggregatorService,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["query"] = ["Search query parameter is required."] }
            );
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        try
        {
            var events = await eventAggregatorService.SearchEventsAsync(query.Trim(), targetCity, cancellationToken);
            return TypedResults.Ok(events);
        }
        catch (AllProvidersUnavailableException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Upstream Providers Unavailable",
                detail: "All external event providers failed to respond."
            );
        }
    }
}
