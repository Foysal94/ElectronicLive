using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
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
        IArtistVerificationService artistVerificationService,
        CancellationToken cancellationToken = default
    )
    {
        if (!request.TryValidate(out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var trimmedArtist = request.ArtistName!.Trim();
        var isVerified = await artistVerificationService.VerifyArtistExistsAsync(trimmedArtist, cancellationToken);
        if (!isVerified)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["artistName"] = [$"Artist '{trimmedArtist}' could not be verified as a genuine music entity."],
                }
            );
        }

        var (subscriptionId, isNew) = await subscriptionService.SubscribeAsync(request, cancellationToken);
        var targetCity = string.IsNullOrWhiteSpace(request.City) ? "London" : request.City.Trim();

        return isNew
            ? TypedResults.Created(
                $"/api/subscriptions/{subscriptionId}",
                new SubscribeResponse(subscriptionId, "Subscribed successfully")
            )
            : TypedResults.Ok(
                new SubscribeResponse(subscriptionId, $"Already subscribed to {trimmedArtist} in {targetCity}.")
            );
    }

    internal static async Task<ContentHttpResult> Unsubscribe(
        string? token,
        string? artist,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken = default
    )
    {
        var (success, message) = await subscriptionService.UnsubscribeAsync(token, artist, cancellationToken);

        if (!success)
        {
            return string.IsNullOrWhiteSpace(token)
                ? SubscriptionHtmlRenderer.BadRequest(message)
                : SubscriptionHtmlRenderer.NotFound(message);
        }

        return message.Contains("not currently subscribed", StringComparison.OrdinalIgnoreCase)
            ? SubscriptionHtmlRenderer.NotSubscribed(message)
            : SubscriptionHtmlRenderer.Success(message);
    }
}
