using System.Security.Cryptography;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Services;

public sealed class SubscriptionService(
    ElectronicLiveDbContext dbContext,
    IArtistVerificationService artistVerificationService
) : ISubscriptionService
{
    public async Task<SubscribeResult> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!request.TryValidate(out var validationErrors))
        {
            return new SubscribeResult(SubscribeStatus.InvalidInput, Errors: validationErrors);
        }

        var trimmedArtist = request.ArtistName!.Trim();
        var isVerified = await artistVerificationService.VerifyArtistExistsAsync(trimmedArtist, cancellationToken);
        if (!isVerified)
        {
            return new SubscribeResult(
                SubscribeStatus.ArtistNotFound,
                Message: $"Artist '{trimmedArtist}' could not be verified as a genuine music entity.",
                Errors: new Dictionary<string, string[]>
                {
                    ["artistName"] = [$"Artist '{trimmedArtist}' could not be verified as a genuine music entity."],
                }
            );
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

            return new SubscribeResult(
                SubscribeStatus.AlreadySubscribed,
                existing.Id,
                $"Already subscribed to {trimmedArtist} in {targetCity}."
            );
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

        return new SubscribeResult(SubscribeStatus.Created, subscription.Id, "Subscribed successfully");
    }

    public async Task<UnsubscribeResult> UnsubscribeAsync(
        string? token,
        string? artist,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new UnsubscribeResult(
                UnsubscribeStatus.MissingToken,
                "Invalid unsubscribe request. An unsubscribe token is required."
            );
        }

        var trimmedToken = token.Trim();
        var user = await dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UnsubscribeToken == trimmedToken, cancellationToken);

        if (user == null)
        {
            return new UnsubscribeResult(UnsubscribeStatus.InvalidToken, "Invalid or expired unsubscribe link.");
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
            }

            return new UnsubscribeResult(
                UnsubscribeStatus.Success,
                $"You have successfully unsubscribed from alerts for {artist.Trim()}."
            );
        }

        var activeSubscriptions = user.Subscriptions.Where(s => s.IsActive).ToList();
        if (activeSubscriptions.Count > 0)
        {
            activeSubscriptions.ForEach(s => s.IsActive = false);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UnsubscribeResult(
            UnsubscribeStatus.Success,
            "You have successfully unsubscribed from all artist alerts."
        );
    }

    // 32-byte cryptographic random entropy (64 hex chars) ensures unsubscribe tokens cannot be enumerated via URL guessing
    private static string GenerateUnsubscribeToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
