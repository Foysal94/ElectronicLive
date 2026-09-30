using System.Net.Http.Headers;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.Services;

public sealed class ResendEmailDispatcher(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailDispatcher> logger
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
        var resendOpts = options.Value;

        var payload = new
        {
            from = resendOpts.FromEmail,
            to = new[] { toEmail },
            subject = $"New shows announced: {artistName}",
            html = htmlContent,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(payload) };

        if (!string.IsNullOrWhiteSpace(resendOpts.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resendOpts.ApiKey);
        }

        var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Failed to send email digest to {ToEmail} via Resend API. Status: {StatusCode}, Error: {ErrorBody}",
                toEmail,
                response.StatusCode,
                errorBody
            );

            throw new HttpRequestException(
                $"Resend API returned error ({response.StatusCode}): {errorBody}",
                null,
                response.StatusCode
            );
        }

        logger.LogInformation(
            "Successfully sent email digest for artist '{Artist}' to {ToEmail} via Resend",
            artistName,
            toEmail
        );
    }
}
