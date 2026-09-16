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
        IEventCacheService eventCacheService,
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
        var targetQuery = query.Trim();
        try
        {
            var events = await eventCacheService.GetOrAddAsync(
                targetQuery,
                targetCity,
                ct => eventAggregatorService.SearchEventsAsync(targetQuery, targetCity, ct),
                cancellationToken
            );
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
