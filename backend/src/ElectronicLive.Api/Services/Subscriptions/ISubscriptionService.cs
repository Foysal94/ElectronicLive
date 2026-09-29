namespace ElectronicLive.Api.Services.Subscriptions;

public interface ISubscriptionService
{
    Task<SubscribeResult> SubscribeAsync(
        string? email,
        string? artistName,
        string? city,
        CancellationToken cancellationToken = default
    );

    Task<UnsubscribeResult> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    );
}
