using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface ISubscriptionService
{
    Task<SubscribeResult> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default);

    Task<UnsubscribeResult> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    );
}
