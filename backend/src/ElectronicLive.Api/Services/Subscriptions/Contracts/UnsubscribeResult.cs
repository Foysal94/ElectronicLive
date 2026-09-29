namespace ElectronicLive.Api.Services.Subscriptions;

public sealed record UnsubscribeResult(UnsubscribeStatus Status, string? Message = null, string? ErrorMessage = null);
