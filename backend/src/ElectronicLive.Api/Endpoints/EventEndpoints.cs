using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Models;
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
        string? artist,
        ITicketmasterClient ticketmasterClient,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["artist"] = ["Artist query parameter is required."] }
            );
        }

        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();
        var events = await ticketmasterClient.SearchEventsAsync(artist.Trim(), targetCity, cancellationToken);
        return TypedResults.Ok(events);
    }
}
