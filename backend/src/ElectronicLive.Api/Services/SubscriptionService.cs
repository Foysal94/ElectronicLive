using System.Security.Cryptography;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Services;

public sealed class SubscriptionService(ElectronicLiveDbContext dbContext) : ISubscriptionService
{
    public async Task<(Guid SubscriptionId, bool IsNew)> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var targetEmail = request.Email!.Trim().ToLowerInvariant();
        var targetArtist = request.ArtistName!.Trim().ToLowerInvariant();
        var targetCity = string.IsNullOrWhiteSpace(request.City) ? "London" : request.City.Trim();

        var user = await dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == targetEmail, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = targetEmail,
                UnsubscribeToken = GenerateUnsubscribeToken(),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            dbContext.Users.Add(user);
        }

        var existing = user.Subscriptions.FirstOrDefault(s =>
            string.Equals(s.ArtistName, targetArtist, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.City, targetCity, StringComparison.OrdinalIgnoreCase)
        );

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return (existing.Id, IsNew: false);
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = targetArtist,
            City = targetCity,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.Subscriptions.Add(subscription);

        await dbContext.SaveChangesAsync(cancellationToken);

        return (subscription.Id, IsNew: true);
    }

    public async Task<(bool Success, string Message)> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (Success: false, Message: "Invalid unsubscribe request. An unsubscribe token is required.");
        }

        var trimmedToken = token.Trim();
        var user = await dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UnsubscribeToken == trimmedToken, cancellationToken);

        if (user == null)
        {
            return (Success: false, Message: "Invalid or expired unsubscribe link.");
        }

        if (!string.IsNullOrWhiteSpace(artist))
        {
            var targetArtist = artist.Trim().ToLowerInvariant();
            var matched = user
                .Subscriptions.Where(s =>
                    string.Equals(s.ArtistName, targetArtist, StringComparison.OrdinalIgnoreCase) && s.IsActive
                )
                .ToList();

            if (matched.Count > 0)
            {
                matched.ForEach(s => s.IsActive = false);
                await dbContext.SaveChangesAsync(cancellationToken);
                return (Success: true, Message: $"You have successfully unsubscribed from alerts for {artist.Trim()}.");
            }

            return (Success: true, Message: $"You are not currently subscribed to alerts for {artist.Trim()}.");
        }

        user.Subscriptions.Where(s => s.IsActive).ToList().ForEach(s => s.IsActive = false);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (Success: true, Message: "You have successfully unsubscribed from all artist alerts.");
    }

    // 32-byte cryptographic random entropy (64 hex chars) ensures unsubscribe tokens cannot be enumerated via URL guessing
    private static string GenerateUnsubscribeToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
