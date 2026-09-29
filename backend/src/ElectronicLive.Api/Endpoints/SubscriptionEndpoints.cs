using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Endpoints;

public static partial class SubscriptionEndpoints
{
    public static RouteGroupBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions");

        group.MapPost("/", Subscribe);
        group.MapGet("/unsubscribe", Unsubscribe);

        return group;
    }

    internal static async Task<Results<Created<SubscribeResponse>, Ok<SubscribeResponse>, ValidationProblem>> Subscribe(
        SubscribeRequest request,
        ElectronicLiveDbContext dbContext,
        IArtistVerificationService artistVerificationService,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsValidEmail(request.Email))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["email"] = ["A valid email address is required."] }
            );
        }

        if (string.IsNullOrWhiteSpace(request.ArtistName))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["artistName"] = ["Artist name is required."] }
            );
        }

        var trimmedArtist = request.ArtistName.Trim();
        var isVerified = await artistVerificationService.VerifyArtistExistsAsync(trimmedArtist, cancellationToken);
        if (!isVerified)
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
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

        var existingSubscription = user.Subscriptions.FirstOrDefault(s =>
            string.Equals(s.ArtistName, targetArtist, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.City, targetCity, StringComparison.OrdinalIgnoreCase)
        );

        if (existingSubscription != null)
        {
            if (existingSubscription.IsActive)
            {
                return TypedResults.Ok(
                    new SubscribeResponse(
                        existingSubscription.Id,
                        $"Already subscribed to {trimmedArtist} in {targetCity}."
                    )
                );
            }

            existingSubscription.IsActive = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return TypedResults.Ok(
                new SubscribeResponse(
                    existingSubscription.Id,
                    $"Subscription to {trimmedArtist} in {targetCity} reactivated successfully."
                )
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/api/subscriptions/{newSubscription.Id}",
            new SubscribeResponse(newSubscription.Id, "Subscribed successfully")
        );
    }

    internal static async Task<ContentHttpResult> Unsubscribe(
        string? token,
        string? artist,
        ElectronicLiveDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return HtmlResponse(
                "Unsubscribe Error",
                "Invalid unsubscribe request. An unsubscribe token is required.",
                isSuccess: false,
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var trimmedToken = token.Trim();
        var user = await dbContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.UnsubscribeToken == trimmedToken, cancellationToken);

        if (user == null)
        {
            return HtmlResponse(
                "Unsubscribe Error",
                "Invalid or expired unsubscribe link.",
                isSuccess: false,
                statusCode: StatusCodes.Status404NotFound
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
                await dbContext.SaveChangesAsync(cancellationToken);
                return HtmlResponse(
                    "Unsubscribed",
                    $"You have successfully unsubscribed from alerts for {artist.Trim()}.",
                    isSuccess: true,
                    statusCode: StatusCodes.Status200OK
                );
            }

            return HtmlResponse(
                "Not Subscribed",
                $"You are not currently subscribed to alerts for {artist.Trim()}.",
                isSuccess: true,
                statusCode: StatusCodes.Status200OK
            );
        }

        foreach (var sub in user.Subscriptions.Where(s => s.IsActive))
        {
            sub.IsActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return HtmlResponse(
            "Unsubscribed",
            "You have successfully unsubscribed from all artist alerts.",
            isSuccess: true,
            statusCode: StatusCodes.Status200OK
        );
    }

    private static ContentHttpResult HtmlResponse(string title, string message, bool isSuccess, int statusCode)
    {
        var html = RenderConfirmationPage(title, message, isSuccess);
        return TypedResults.Content(html, "text/html; charset=utf-8", Encoding.UTF8, statusCode: statusCode);
    }

    private static string GenerateUnsubscribeToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

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

    private static string RenderConfirmationPage(string title, string message, bool isSuccess)
    {
        var icon = isSuccess ? "✅" : "⚠️";
        var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
        var encodedMessage = System.Net.WebUtility.HtmlEncode(message);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>ElectronicLive - {{encodedTitle}}</title>
              <style>
                body {
                  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                  background-color: #0f172a;
                  color: #f8fafc;
                  display: flex;
                  justify-content: center;
                  align-items: center;
                  min-height: 100vh;
                  margin: 0;
                  padding: 1rem;
                  box-sizing: border-box;
                }
                .card {
                  background-color: #1e293b;
                  border: 1px solid #334155;
                  border-radius: 12px;
                  padding: 2rem;
                  max-width: 480px;
                  width: 100%;
                  text-align: center;
                  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3);
                }
                .icon {
                  font-size: 2.5rem;
                  margin-bottom: 1rem;
                }
                h1 {
                  font-size: 1.5rem;
                  font-weight: 600;
                  margin: 0 0 0.75rem 0;
                  color: #f8fafc;
                }
                p {
                  color: #94a3b8;
                  font-size: 1rem;
                  line-height: 1.5;
                  margin: 0 0 1.5rem 0;
                }
                .brand {
                  font-size: 0.875rem;
                  color: #64748b;
                  font-weight: 500;
                }
              </style>
            </head>
            <body>
              <div class="card">
                <div class="icon">{{icon}}</div>
                <h1>{{encodedTitle}}</h1>
                <p>{{encodedMessage}}</p>
                <div class="brand">ElectronicLive London EDM Tracker</div>
              </div>
            </body>
            </html>
            """;
    }

    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex EmailRegex();
}
