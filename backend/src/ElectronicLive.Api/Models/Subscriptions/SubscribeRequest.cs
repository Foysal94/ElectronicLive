namespace ElectronicLive.Api.Models.Subscriptions;

public sealed record SubscribeRequest(string? Email, string? ArtistName, string? City = "London");
