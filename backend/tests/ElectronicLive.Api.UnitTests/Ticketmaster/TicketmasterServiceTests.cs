using System.Net;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests;

public class TicketmasterServiceTests
{
    [Fact]
    public async Task SearchEventsAsync_ReturnsEmpty_WhenApiKeyMissing()
    {
        var (service, handler) = CreateService(apiKey: string.Empty);

        var result = await service.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task SearchEventsAsync_ConstructsExpectedRequestUrl()
    {
        var (service, handler) = CreateService();

        await service.SearchEventsAsync("Bicep & Hammer", "London & South");

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);

        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("apikey=test-key");
        query.ShouldContain("keyword=Bicep%20%26%20Hammer");
        query.ShouldContain("city=London%20%26%20South");
        query.ShouldContain("countryCode=GB");
        query.ShouldContain("classificationName=music");
        query.ShouldContain("sort=date,asc");
    }

    [Fact]
    public async Task SearchEventsAsync_DefaultsCityToLondon_WhenCityNullOrWhitespace()
    {
        var (service, handler) = CreateService();

        await service.SearchEventsAsync("Bicep", "   ");

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.ToString().ShouldContain("city=London");
    }

    [Fact]
    public async Task SearchEventsAsync_ForwardsCancellationTokenToHttpHandler()
    {
        var (service, handler) = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await service.SearchEventsAsync("Bicep", cancellationToken: cts.Token);

        result.ShouldBeEmpty();
        handler.WasCanceledDuringSend.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchEventsAsync_ParsesHalJson_AndMapsPropertiesCorrectly()
    {
        const string json = """
            {
              "_embedded": {
                "events": [
                  {
                    "id": "event-1",
                    "name": "Bicep Live",
                    "url": "https://ticketmaster.co.uk/event1",
                    "dates": {
                      "start": {
                        "localDate": "2026-11-26",
                        "localTime": "18:00:00"
                      },
                      "status": {
                        "code": "onsale"
                      }
                    },
                    "_embedded": {
                      "venues": [
                        { "name": "Royal Albert Hall" }
                      ]
                    }
                  }
                ]
              }
            }
            """;

        var (service, _) = CreateService(responseBody: json);

        var result = await service.SearchEventsAsync("Bicep");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("event-1");
        ev.Name.ShouldBe("Bicep Live");
        ev.VenueName.ShouldBe("Royal Albert Hall");
        ev.Date.ShouldBe("2026-11-26");
        ev.Time.ShouldBe("18:00:00");
        ev.TicketUrl.ShouldBe("https://ticketmaster.co.uk/event1");
        ev.Status.ShouldBe(EventStatus.OnSale);
    }

    [Fact]
    public async Task SearchEventsAsync_ReturnsEmpty_WhenEmbeddedOmitted()
    {
        const string json = """
            {
              "page": {
                "size": 20,
                "totalElements": 0
              }
            }
            """;

        var (service, _) = CreateService(responseBody: json);

        var result = await service.SearchEventsAsync("NonExistentArtist");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchEventsAsync_FallsBackToUnknownVenue_WhenVenuesEmpty()
    {
        const string json = """
            {
              "_embedded": {
                "events": [
                  {
                    "id": "ev-2",
                    "name": "Secret Gig",
                    "_embedded": {
                      "venues": []
                    }
                  }
                ]
              }
            }
            """;

        var (service, _) = CreateService(responseBody: json);

        var result = await service.SearchEventsAsync("Secret");

        var ev = result.ShouldHaveSingleItem();
        ev.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public async Task SearchEventsAsync_ReturnsEmpty_WhenHttpFails()
    {
        var (service, _) = CreateService(statusCode: HttpStatusCode.InternalServerError);

        var result = await service.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    private static (TicketmasterService Service, CapturingHttpMessageHandler Handler) CreateService(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}",
        string apiKey = "test-key"
    )
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = apiKey });
        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.ticketmaster.com/discovery/v2/"),
        };
        var service = new TicketmasterService(httpClient, options, NullLogger<TicketmasterService>.Instance);
        return (service, handler);
    }

    private sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public bool WasCanceledDuringSend { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastRequest = request;
            WasCanceledDuringSend = cancellationToken.IsCancellationRequested;
            return Task.FromResult(handler(request));
        }
    }
}
