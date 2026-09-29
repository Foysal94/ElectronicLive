using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Services;

public sealed partial class SubscriptionService : ISubscriptionService
{
    private readonly ElectronicLiveDbContext _dbContext;
    private readonly IArtistVerificationService _artistVerificationService;

    public SubscriptionService(ElectronicLiveDbContext dbContext, IArtistVerificationService artistVerificationService)
    {
        _dbContext = dbContext;
        _artistVerificationService = artistVerificationService;
    }

    public async Task<SubscribeResult> SubscribeAsync(
        string? email,
        string? artistName,
        string? city,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsValidEmail(email))
        {
            return new SubscribeResult(
                SubscribeStatus.InvalidEmail,
                ErrorMessage: "A valid email address is required."
            );
        }

        if (string.IsNullOrWhiteSpace(artistName))
        {
            return new SubscribeResult(SubscribeStatus.InvalidArtist, ErrorMessage: "Artist name is required.");
        }

        var trimmedArtist = artistName.Trim();
        var isVerified = await _artistVerificationService.VerifyArtistExistsAsync(trimmedArtist, cancellationToken);
        if (!isVerified)
        {
            return new SubscribeResult(
                SubscribeStatus.ArtistNotVerified,
                ErrorMessage: $"Artist '{trimmedArtist}' could not be verified as a genuine music entity."
            );
        }

        var targetEmail = email!.Trim().ToLowerInvariant();
        var targetArtist = trimmedArtist.ToLowerInvariant();
        var targetCity = string.IsNullOrWhiteSpace(city) ? "London" : city.Trim();

        var user = await _dbContext
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
            _dbContext.Users.Add(user);
        }

        var existingSubscription = user.Subscriptions.FirstOrDefault(s =>
            string.Equals(s.ArtistName, targetArtist, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.City, targetCity, StringComparison.OrdinalIgnoreCase)
        );

        if (existingSubscription != null)
        {
            if (existingSubscription.IsActive)
            {
                return new SubscribeResult(
                    SubscribeStatus.AlreadySubscribed,
                    existingSubscription.Id,
                    $"Already subscribed to {trimmedArtist} in {targetCity}."
                );
            }

            existingSubscription.IsActive = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new SubscribeResult(
                SubscribeStatus.Reactivated,
                existingSubscription.Id,
                $"Subscription to {trimmedArtist} in {targetCity} reactivated successfully."
            );
        }

        var newSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = targetArtist,
            City = targetCity,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.Subscriptions.Add(newSubscription);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubscribeResult(SubscribeStatus.Created, newSubscription.Id, "Subscribed successfully");
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
                ErrorMessage: "Invalid unsubscribe request. An unsubscribe token is required."
            );
        }

        var trimmedToken = token.Trim();
        var user = await _dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UnsubscribeToken == trimmedToken, cancellationToken);

        if (user == null)
        {
            return new UnsubscribeResult(
                UnsubscribeStatus.InvalidToken,
                ErrorMessage: "Invalid or expired unsubscribe link."
            );
        }

        if (!string.IsNullOrWhiteSpace(artist))
        {
            var targetArtist = artist.Trim().ToLowerInvariant();
            var matchingSubscriptions = user
                .Subscriptions.Where(s =>
                    string.Equals(s.ArtistName, targetArtist, StringComparison.OrdinalIgnoreCase) && s.IsActive
                )
                .ToList();

            if (matchingSubscriptions.Count > 0)
            {
                foreach (var sub in matchingSubscriptions)
                {
                    sub.IsActive = false;
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new UnsubscribeResult(
                    UnsubscribeStatus.Success,
                    Message: $"You have successfully unsubscribed from alerts for {artist.Trim()}."
                );
            }

            return new UnsubscribeResult(
                UnsubscribeStatus.NotSubscribed,
                Message: $"You are not currently subscribed to alerts for {artist.Trim()}."
            );
        }

        foreach (var sub in user.Subscriptions.Where(s => s.IsActive))
        {
            sub.IsActive = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UnsubscribeResult(
            UnsubscribeStatus.AllSuccess,
            Message: "You have successfully unsubscribed from all artist alerts."
        );
    }

    private static string GenerateUnsubscribeToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        if (!EmailRegex().IsMatch(trimmed))
        {
            return false;
        }

        try
        {
            var mailAddress = new MailAddress(trimmed);
            return mailAddress.Address == trimmed;
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex EmailRegex();
}
