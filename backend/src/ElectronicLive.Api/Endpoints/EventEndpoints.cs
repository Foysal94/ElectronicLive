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
        string? genre,
        IEventAggregatorService eventAggregatorService,
        IEventCacheService eventCacheService,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(genre))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["query"] = ["At least one of query or genre parameter is required."],
                }
            );
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var targetQuery = query?.Trim();
        var targetGenre = genre?.Trim().ToLowerInvariant();
        try
        {
            // Explicitly orchestrate caching here rather than via decorator wrapping to maintain
            // linear, top-to-bottom execution flow and obvious dependency resolution.
            var events = await eventCacheService.GetOrAddAsync(
                targetQuery,
                targetGenre,
                targetCity,
                ct => eventAggregatorService.SearchEventsAsync(targetQuery, targetGenre, targetCity, ct),
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
