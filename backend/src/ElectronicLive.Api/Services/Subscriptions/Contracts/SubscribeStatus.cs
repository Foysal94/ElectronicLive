namespace ElectronicLive.Api.Services.Subscriptions;

public enum SubscribeStatus
{
    Created,
    AlreadySubscribed,
    Reactivated,
    InvalidEmail,
    InvalidArtist,
    ArtistNotVerified,
}
