namespace ElectronicLive.Api.Services.Subscriptions;

public sealed record SubscribeResult(
    SubscribeStatus Status,
    Guid SubscriptionId = default,
    string? Message = null,
    string? ErrorMessage = null
);
