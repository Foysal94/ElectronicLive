using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;

namespace ElectronicLive.Api.UnitTests.Services;

public sealed class EmailTemplateBuilderTests
{
    [Fact]
    public void Should_RenderBrandHeaderAndDigestDate_InHtml()
    {
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Bicep Live at Drumsheds",
                "Drumsheds",
                new DateOnly(2026, 11, 15),
                new TimeOnly(22, 0),
                "https://ra.co/events/123",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };

        var html = EmailTemplateBuilder.BuildDigestHtml(
            "Bicep",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=abc"
        );

        html.ShouldContain("ElectronicLive");
        html.ShouldContain("Bicep");
        html.ShouldContain("#0f172a");
        html.ShouldContain("#1e293b");
    }

    [Fact]
    public void Should_RenderEventCards_WithDateVenueAndProvider()
    {
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Bicep Chroma AV DJ Set",
                "Printworks London",
                new DateOnly(2026, 12, 5),
                new TimeOnly(21, 30),
                "https://ticketmaster.co.uk/event/1",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
            new(
                "evt-2",
                "Bicep Live",
                "Fabric",
                new DateOnly(2026, 12, 12),
                null,
                "https://skiddle.com/events/2",
                EventStatus.OnSale,
                EventProvider.Skiddle
            ),
        };

        var html = EmailTemplateBuilder.BuildDigestHtml(
            "Bicep",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=abc"
        );

        html.ShouldContain("Bicep Chroma AV DJ Set");
        html.ShouldContain("Printworks London");
        html.ShouldContain("5 December 2026");
        html.ShouldContain("Bicep Live");
        html.ShouldContain("Fabric");
        html.ShouldContain("12 December 2026");
    }

    [Fact]
    public void Should_RenderMultiProviderTicketButtons_WithCorrectLinksAndStatus()
    {
        var offers = new List<EventTicketOffer>
        {
            new(EventProvider.Ticketmaster, "https://ticketmaster.co.uk/event/100", EventStatus.OnSale),
            new(EventProvider.ResidentAdvisor, "https://ra.co/events/200", EventStatus.SoldOut),
            new(EventProvider.Skiddle, "https://skiddle.com/events/300", EventStatus.OnSale),
        };

        var events = new List<EventResponse>
        {
            new(
                "evt-multi",
                "Overmono Live",
                "Roundhouse",
                new DateOnly(2026, 10, 20),
                new TimeOnly(19, 0),
                "https://ticketmaster.co.uk/event/100",
                EventStatus.OnSale,
                EventProvider.Ticketmaster,
                offers
            ),
        };

        var html = EmailTemplateBuilder.BuildDigestHtml(
            "Overmono",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=abc"
        );

        html.ShouldContain("https://ticketmaster.co.uk/event/100");
        html.ShouldContain("Ticketmaster");
        html.ShouldContain("https://skiddle.com/events/300");
        html.ShouldContain("Skiddle");
        html.ShouldContain("Resident Advisor");
        html.ShouldContain("Sold Out");
    }

    [Fact]
    public void Should_RenderUnsubscribeLink_InFooter()
    {
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Bonobo DJ Set",
                "KOKO",
                new DateOnly(2026, 11, 1),
                null,
                "https://ra.co/events/bonobo",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };

        const string unsubscribeUrl =
            "https://electroniclive.co.uk/api/subscriptions/unsubscribe?token=sec-token-123&artist=bonobo";
        var html = EmailTemplateBuilder.BuildDigestHtml("Bonobo", events, unsubscribeUrl);

        html.ShouldContain(
            "https://electroniclive.co.uk/api/subscriptions/unsubscribe?token=sec-token-123&amp;artist=bonobo"
        );
        html.ShouldContain("Unsubscribe");
    }

    [Fact]
    public void Should_HtmlEncodeDynamicFields_ToPreventXss()
    {
        var events = new List<EventResponse>
        {
            new(
                "evt-xss",
                "<script>alert('xss-name')</script>",
                "<b>Venue</b> & Co",
                new DateOnly(2026, 10, 1),
                null,
                "https://example.com/tickets",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };

        var html = EmailTemplateBuilder.BuildDigestHtml(
            "<img src=x onerror=alert(1)>",
            events,
            "https://example.com/unsub?q=<test>&b=1"
        );

        html.ShouldNotContain("<script>alert('xss-name')</script>");
        html.ShouldNotContain("<img src=x onerror=alert(1)>");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("&lt;img src=x");
    }
}
