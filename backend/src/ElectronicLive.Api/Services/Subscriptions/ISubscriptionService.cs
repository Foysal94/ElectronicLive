using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services.Subscriptions;

public interface ISubscriptionService
{
    Task<SubscribeOutcome?> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);

    Task<UnsubscribeOutcome> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    );
}
