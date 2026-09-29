namespace ElectronicLive.Api.Services;

public enum SubscribeStatus
{
    Created,
    AlreadySubscribed,
    Reactivated,
    InvalidEmail,
    InvalidArtist,
    ArtistNotVerified,
}

public sealed record SubscribeResult(
    SubscribeStatus Status,
    Guid SubscriptionId = default,
    string? Message = null,
    string? ErrorMessage = null
);

public enum UnsubscribeStatus
{
    Success,
    AllSuccess,
    NotSubscribed,
    InvalidToken,
    MissingToken,
}

public sealed record UnsubscribeResult(UnsubscribeStatus Status, string? Message = null, string? ErrorMessage = null);

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
