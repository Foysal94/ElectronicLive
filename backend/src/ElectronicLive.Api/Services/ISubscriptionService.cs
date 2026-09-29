using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface ISubscriptionService
{
    Task<(Guid SubscriptionId, bool IsNew)> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default
    );

    Task<(bool Success, string Message)> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    );
}
