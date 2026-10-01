namespace ElectronicLive.Api.Services.Subscriptions;

public sealed record SubscribeOutcome(Guid SubscriptionId, bool IsNew);
