using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services.Subscriptions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static RouteGroupBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions");

        group.MapPost("/", Subscribe);
        group.MapGet("/unsubscribe", Unsubscribe);

        return group;
    }

    internal static async Task<Results<Created<SubscribeResponse>, Ok<SubscribeResponse>, ValidationProblem>> Subscribe(
        SubscribeRequest request,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken = default
    )
    {
        if (!request.TryValidate(out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var outcome = await subscriptionService.SubscribeAsync(request, cancellationToken);
        if (outcome is null)
        {
            var trimmedArtist = request.ArtistName?.Trim() ?? string.Empty;
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["artistName"] = [$"Artist '{trimmedArtist}' could not be verified as a genuine music entity."],
                }
            );
        }

        var artist = request.ArtistName!.Trim();
        var city = string.IsNullOrWhiteSpace(request.City) ? "London" : request.City.Trim();

        return outcome.IsNew
            ? TypedResults.Created(
                $"/api/subscriptions/{outcome.SubscriptionId}",
                new SubscribeResponse(outcome.SubscriptionId, "Subscribed successfully")
            )
            : TypedResults.Ok(
                new SubscribeResponse(outcome.SubscriptionId, $"Already subscribed to {artist} in {city}.")
            );
    }

    internal static async Task<ContentHttpResult> Unsubscribe(
        string? token,
        string? artist,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await subscriptionService.UnsubscribeAsync(token, artist, cancellationToken);

        return outcome switch
        {
            UnsubscribeOutcome.Success => SubscriptionHtmlRenderer.Success(artist),
            UnsubscribeOutcome.MissingToken => SubscriptionHtmlRenderer.BadRequest(),
            _ => SubscriptionHtmlRenderer.NotFound(),
        };
    }
}
