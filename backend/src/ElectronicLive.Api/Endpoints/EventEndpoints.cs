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
        IEventSearchService eventSearchService,
        DateOnly? from = null,
        DateOnly? to = null,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        var hasGenre = !string.IsNullOrWhiteSpace(genre);

        if (from.HasValue && to.HasValue && to.Value < from.Value)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["to"] = ["'to' date must be greater than or equal to 'from' date."],
                }
            );
        }

        if (!hasQuery && !hasGenre)
        {
            if (from == null && to == null)
            {
                return TypedResults.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["query"] = ["At least one of query or genre parameter is required."],
                    }
                );
            }

            if (from == null || to == null)
            {
                return TypedResults.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["date"] = ["Both 'from' and 'to' date parameters are required for date-only queries."],
                    }
                );
            }

            if (to.Value.DayNumber - from.Value.DayNumber > 7)
            {
                return TypedResults.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["date"] = ["Date range cannot exceed 7 days when searching without query or genre."],
                    }
                );
            }
        }

        if (hasGenre && !EventGenres.IsValid(genre))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["genre"] =
                    [
                        $"Invalid genre '{genre}'. Supported genres are: {string.Join(", ", EventGenres.All)}.",
                    ],
                }
            );
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var targetQuery = query?.Trim();
        var targetGenre = genre?.Trim().ToLowerInvariant();

        try
        {
            var events = await eventSearchService.SearchEventsAsync(
                targetQuery,
                targetGenre,
                targetCity,
                from,
                to,
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
