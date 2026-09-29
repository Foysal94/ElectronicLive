using ElectronicLive.Api.Models.Subscriptions;
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
        var result = await subscriptionService.SubscribeAsync(
            request.Email,
            request.ArtistName,
            request.City,
            cancellationToken
        );

        return result.Status switch
        {
            SubscribeStatus.Created => TypedResults.Created(
                $"/api/subscriptions/{result.SubscriptionId}",
                new SubscribeResponse(result.SubscriptionId, result.Message ?? "Subscribed successfully")
            ),
            SubscribeStatus.AlreadySubscribed or SubscribeStatus.Reactivated => TypedResults.Ok(
                new SubscribeResponse(result.SubscriptionId, result.Message!)
            ),
            SubscribeStatus.InvalidEmail => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["email"] = [result.ErrorMessage!] }
            ),
            SubscribeStatus.InvalidArtist or SubscribeStatus.ArtistNotVerified => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["artistName"] = [result.ErrorMessage!] }
            ),
            _ => throw new InvalidOperationException($"Unexpected subscribe status: {result.Status}"),
        };
    }

    internal static async Task<ContentHttpResult> Unsubscribe(
        string? token,
        string? artist,
        ISubscriptionService subscriptionService,
        CancellationToken cancellationToken = default
    )
    {
        var result = await subscriptionService.UnsubscribeAsync(token, artist, cancellationToken);

        return result.Status switch
        {
            UnsubscribeStatus.MissingToken => SubscriptionHtmlRenderer.BadRequest(result.ErrorMessage!),
            UnsubscribeStatus.InvalidToken => SubscriptionHtmlRenderer.NotFound(result.ErrorMessage!),
            UnsubscribeStatus.Success or UnsubscribeStatus.AllSuccess => SubscriptionHtmlRenderer.Success(
                result.Message!
            ),
            UnsubscribeStatus.NotSubscribed => SubscriptionHtmlRenderer.NotSubscribed(result.Message!),
            _ => throw new InvalidOperationException($"Unexpected unsubscribe status: {result.Status}"),
        };
    }
}
