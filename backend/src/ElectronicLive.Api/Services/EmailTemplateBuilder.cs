using System.Globalization;
using System.Net;
using System.Text;
using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public static class EmailTemplateBuilder
{
    private static readonly Lazy<string> CachedTemplate = new(LoadTemplate);

    public static string BuildDigestHtml(string artistName, IReadOnlyList<EventResponse> events, string unsubscribeUrl)
    {
        var rawTemplate = CachedTemplate.Value;
        var encodedArtist = WebUtility.HtmlEncode(artistName);
        var encodedUnsubscribe = WebUtility.HtmlEncode(unsubscribeUrl);
        var digestDate = DateTimeOffset.UtcNow.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        var cardsBuilder = new StringBuilder();
        foreach (var evt in events)
        {
            cardsBuilder.Append(RenderEventCard(evt));
        }

        return rawTemplate
            .Replace("{{ArtistName}}", encodedArtist)
            .Replace("{{DigestDate}}", digestDate)
            .Replace("{{EventCards}}", cardsBuilder.ToString())
            .Replace("{{UnsubscribeUrl}}", encodedUnsubscribe);
    }

    private static string RenderEventCard(EventResponse evt)
    {
        var encodedName = WebUtility.HtmlEncode(evt.Name);
        var encodedVenue = WebUtility.HtmlEncode(evt.VenueName);

        var dateString = evt.Date.HasValue
            ? evt.Date.Value.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)
            : "Date TBA";

        if (evt.Time.HasValue)
        {
            dateString += string.Create(CultureInfo.InvariantCulture, $" &bull; {evt.Time.Value:HH:mm}");
        }

        var buttonsHtml = RenderTicketButtons(evt);

        return string.Create(
            CultureInfo.InvariantCulture,
            $$"""
            <tr>
              <td style="padding-bottom: 16px;">
                <table border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #1e293b; border: 1px solid #334155; border-radius: 12px; overflow: hidden;">
                  <tr>
                    <td class="card-content" style="padding: 20px;">
                      <div style="font-size: 13px; font-weight: 600; color: #38bdf8; margin-bottom: 6px;">
                        📅 {{dateString}}
                      </div>
                      <div style="font-size: 18px; font-weight: 700; color: #f8fafc; margin-bottom: 6px; line-height: 1.3;">
                        {{encodedName}}
                      </div>
                      <div style="font-size: 14px; color: #94a3b8; margin-bottom: 16px;">
                        📍 {{encodedVenue}}
                      </div>
                      <table border="0" cellpadding="0" cellspacing="0">
                        <tr>
                          <td>
                            {{buttonsHtml}}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            """
        );
    }

    private static string RenderTicketButtons(EventResponse evt)
    {
        var offers =
            evt.Offers != null && evt.Offers.Count > 0
                ? evt.Offers
                : [new EventTicketOffer(evt.Provider, evt.TicketUrl, evt.Status)];

        var sb = new StringBuilder();

        foreach (var offer in offers)
        {
            var (providerName, buttonColor) = GetProviderMetadata(offer.Provider);
            var isSoldOut = offer.Status == EventStatus.SoldOut;
            var ticketUrl = offer.TicketUrl;

            if (isSoldOut)
            {
                sb.Append(
                    CultureInfo.InvariantCulture,
                    $$"""
                    <span style="display: inline-block; margin-right: 8px; margin-bottom: 8px; padding: 8px 14px; font-size: 13px; font-weight: 600; color: #94a3b8; background-color: #334155; border-radius: 6px; text-decoration: none;">
                      {{providerName}} (Sold Out)
                    </span>
                    """
                );
            }
            else if (!string.IsNullOrWhiteSpace(ticketUrl))
            {
                var encodedUrl = WebUtility.HtmlEncode(ticketUrl);

                sb.Append(
                    CultureInfo.InvariantCulture,
                    $$"""
                    <a href="{{encodedUrl}}" class="ticket-button" style="display: inline-block; margin-right: 8px; margin-bottom: 8px; padding: 8px 14px; font-size: 13px; font-weight: 600; color: #ffffff; background-color: {{buttonColor}}; border-radius: 6px; text-decoration: none; text-align: center;">
                      Buy on {{providerName}} &rarr;
                    </a>
                    """
                );
            }
        }

        return sb.ToString();
    }

    private static (string Name, string Color) GetProviderMetadata(EventProvider provider) =>
        provider switch
        {
            EventProvider.Ticketmaster => ("Ticketmaster", "#026cdf"),
            EventProvider.ResidentAdvisor => ("Resident Advisor", "#059669"),
            EventProvider.Skiddle => ("Skiddle", "#7c3aed"),
            _ => (provider.ToString(), "#2563eb"),
        };

    private static string LoadTemplate()
    {
        var assembly = typeof(EmailTemplateBuilder).Assembly;
        const string resourceName = "ElectronicLive.Api.Templates.DigestEmailTemplate.html";

        using var stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found in assembly {assembly.FullName}."
            );

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
