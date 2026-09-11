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

    internal static async Task<Results<Ok<IReadOnlyList<EventDto>>, BadRequest<string>>> SearchEvents(
        string? artist,
        ITicketmasterService ticketmasterService,
        string? city = "London",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            return TypedResults.BadRequest("Artist query parameter is required.");
        }

        var effectiveCity = string.IsNullOrWhiteSpace(city) ? "London" : city;
        var events = await ticketmasterService.SearchEventsAsync(artist, effectiveCity, cancellationToken);
        return TypedResults.Ok(events);
    }
}
