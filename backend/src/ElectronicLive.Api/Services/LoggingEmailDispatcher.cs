using System.Text.RegularExpressions;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public sealed partial class LoggingEmailDispatcher(
    ILogger<LoggingEmailDispatcher> logger,
    string scratchDirectory = ".scratch/emails"
) : IEmailDispatcher
{
    public async Task SendDigestAsync(
        string toEmail,
        string artistName,
        IReadOnlyList<EventResponse> newEvents,
        string unsubscribeUrl,
        CancellationToken ct = default
    )
    {
        var htmlContent = EmailTemplateBuilder.BuildDigestHtml(artistName, newEvents, unsubscribeUrl);

        logger.LogInformation(
            "Dispatched email digest for artist '{Artist}' ({EventCount} events) to {ToEmail}",
            artistName,
            newEvents.Count,
            toEmail
        );

        Directory.CreateDirectory(scratchDirectory);

        var sanitizedArtist = SanitizeFileName().Replace(artistName.ToLowerInvariant(), "-");
        var timestamp = DateTimeOffset.UtcNow.ToString(
            "yyyyMMdd-HHmmssfff",
            System.Globalization.CultureInfo.InvariantCulture
        );
        var filePath = Path.Combine(scratchDirectory, $"{timestamp}_{sanitizedArtist}.html");

        await File.WriteAllTextAsync(filePath, htmlContent, ct);
    }

    [GeneratedRegex(@"[^a-z0-9_-]+")]
    private static partial Regex SanitizeFileName();
}
