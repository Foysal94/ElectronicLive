namespace ElectronicLive.Api.Services;

public sealed record SubscribeResult(
    SubscribeStatus Status,
    Guid? SubscriptionId = null,
    string Message = "",
    IReadOnlyDictionary<string, string[]>? Errors = null
);
