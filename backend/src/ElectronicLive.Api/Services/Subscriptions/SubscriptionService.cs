using System.Security.Cryptography;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Services.Subscriptions;

public sealed class SubscriptionService(
    ElectronicLiveDbContext dbContext,
    IArtistVerificationService artistVerificationService
) : ISubscriptionService
{
    public async Task<SubscribeOutcome?> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var trimmedArtist = request.ArtistName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedArtist))
        {
            return null;
        }

        var isVerified = await artistVerificationService.VerifyArtistExistsAsync(trimmedArtist, cancellationToken);
        if (!isVerified)
        {
            return null;
        }

        var targetEmail = request.Email!.Trim().ToLowerInvariant();
        var targetArtist = trimmedArtist.ToLowerInvariant();
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

            return new SubscribeOutcome(existing.Id, IsNew: false);
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

        return new SubscribeOutcome(subscription.Id, IsNew: true);
    }

    public async Task<UnsubscribeOutcome> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return UnsubscribeOutcome.MissingToken;
        }

        var trimmedToken = token.Trim();
        var user = await dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UnsubscribeToken == trimmedToken, cancellationToken);

        if (user == null)
        {
            return UnsubscribeOutcome.InvalidToken;
        }

        var targetSubscriptions = !string.IsNullOrWhiteSpace(artist)
            ? user.Subscriptions.Where(s =>
                string.Equals(s.ArtistName, artist.Trim(), StringComparison.OrdinalIgnoreCase) && s.IsActive
            )
            : user.Subscriptions.Where(s => s.IsActive);

        var toDeactivate = targetSubscriptions.ToList();
        if (toDeactivate.Count > 0)
        {
            toDeactivate.ForEach(s => s.IsActive = false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return UnsubscribeOutcome.Success;
    }

    // 32-byte cryptographic random entropy (64 hex chars) ensures unsubscribe tokens cannot be enumerated via URL guessing
    private static string GenerateUnsubscribeToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
