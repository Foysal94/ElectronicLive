using System.Net;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests;

public class TicketmasterServiceTests
{
    private readonly NullLogger<TicketmasterService> _logger = NullLogger<TicketmasterService>.Instance;

    [Fact]
    public async Task SearchEventsAsync_ReturnsEmpty_WhenApiKeyMissing()
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = string.Empty });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

        var result = await service.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
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

        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

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

        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

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

        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

        var result = await service.SearchEventsAsync("Secret");

        var ev = result.ShouldHaveSingleItem();
        ev.VenueName.ShouldBe("Unknown Venue");
    }

    [Theory]
    [InlineData("onsale", EventStatus.OnSale)]
    [InlineData("offsale", EventStatus.SoldOut)]
    [InlineData("canceled", EventStatus.Cancelled)]
    [InlineData("cancelled", EventStatus.Cancelled)]
    [InlineData("postponed", EventStatus.Postponed)]
    [InlineData("rescheduled", EventStatus.Postponed)]
    [InlineData("something_else", EventStatus.Unknown)]
    [InlineData(null, EventStatus.Unknown)]
    public async Task SearchEventsAsync_MapsStatusCorrectly(string? statusCode, EventStatus expectedStatus)
    {
        var statusJson = statusCode is null ? "{}" : $"{{\"code\": \"{statusCode}\"}}";
        var json = $$"""
            {
              "_embedded": {
                "events": [
                  {
                    "id": "ev-status",
                    "name": "Test Event",
                    "dates": {
                      "status": {{statusJson}}
                    }
                  }
                ]
              }
            }
            """;

        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

        var result = await service.SearchEventsAsync("Test");

        var ev = result.ShouldHaveSingleItem();
        ev.Status.ShouldBe(expectedStatus);
    }

    [Fact]
    public async Task SearchEventsAsync_ReturnsEmpty_WhenHttpFails()
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        var service = new TicketmasterService(httpClient, options, _logger);

        var result = await service.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(handler(request));
        }
    }
}
