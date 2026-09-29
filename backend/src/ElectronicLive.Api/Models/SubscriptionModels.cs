namespace ElectronicLive.Api.Models;

public sealed record SubscribeRequest(string? Email, string? ArtistName, string? City = "London");

public sealed record SubscribeResponse(Guid SubscriptionId, string Message);
