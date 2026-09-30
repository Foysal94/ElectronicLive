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
        CancellationToken cancellationToken = default
    )
    {
        if (!request.TryValidate(out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await subscriptionService.SubscribeAsync(request, cancellationToken);

        return result.Status switch
        {
            SubscribeStatus.Created => TypedResults.Created(
                $"/api/subscriptions/{result.SubscriptionId}",
                new SubscribeResponse(result.SubscriptionId!.Value, result.Message)
            ),
            SubscribeStatus.AlreadySubscribed => TypedResults.Ok(
                new SubscribeResponse(result.SubscriptionId!.Value, result.Message)
            ),
            _ => TypedResults.ValidationProblem(
                result.Errors?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string[]>()
            ),
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
            UnsubscribeStatus.Success => SubscriptionHtmlRenderer.Success(result.Message),
            UnsubscribeStatus.MissingToken => SubscriptionHtmlRenderer.BadRequest(result.Message),
            _ => SubscriptionHtmlRenderer.NotFound(result.Message),
        };
    }
}
