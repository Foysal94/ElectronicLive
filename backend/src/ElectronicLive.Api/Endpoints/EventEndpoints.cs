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

    internal static async Task<Results<Ok<IReadOnlyList<EventResponse>>, ValidationProblem>> SearchEvents(
        string? query,
        string? artist,
        IEventAggregatorService eventAggregatorService,
        string? q = null,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        // Fall back to short alias or legacy artist parameter to preserve backward compatibility.
        var searchTerm = query;
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = string.IsNullOrWhiteSpace(q) ? artist : q;
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["query"] = ["Search query parameter is required."] }
            );
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var events = await eventAggregatorService.SearchEventsAsync(searchTerm.Trim(), targetCity, cancellationToken);
        return TypedResults.Ok(events);
    }
}
