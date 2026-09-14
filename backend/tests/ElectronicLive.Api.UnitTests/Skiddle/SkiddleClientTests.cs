using System.Net;
using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.Skiddle;

public class SkiddleClientTests
{
    [Fact]
    public async Task Should_ReturnEmpty_WhenApiKeyMissing()
    {
        var (client, handler) = CreateClient(apiKey: string.Empty);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_ConstructExpectedRequestUrl_WithLondonCoordinatesDefault()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep & Hammer");

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);

        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("api_key=test-key");
        query.ShouldContain("keyword=Bicep%20%26%20Hammer");
        query.ShouldContain("latitude=51.5074");
        query.ShouldContain("longitude=-0.1278");
        query.ShouldContain("radius=25");
        query.ShouldContain("order=date");
    }

    [Fact]
    public async Task Should_ConstructExpectedRequestUrl_WithCustomCityCoordinates()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep", "Manchester");

        handler.LastRequest.ShouldNotBeNull();
        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("latitude=53.4808");
        query.ShouldContain("longitude=-2.2426");
    }

    [Fact]
    public async Task Should_ForwardCancellationTokenToHttpHandler()
    {
        var (client, handler) = CreateClient();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.SearchEventsAsync("Bicep", cancellationToken: cts.Token)
        );
        handler.WasCanceledDuringSend.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenHttpRequestTimesOut()
    {
        var options = Options.Create(new SkiddleOptions { ApiKey = "test-key" });
        var handler = new CapturingHttpMessageHandler(_ => throw new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.skiddle.com/api/v1/") };
        var client = new SkiddleClient(httpClient, options, NullLogger<SkiddleClient>.Instance);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenHttpFails()
    {
        var (client, _) = CreateClient(statusCode: HttpStatusCode.InternalServerError);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ParseJson_AndMapPropertiesCorrectly()
    {
        const string json = """
            {
              "error": 0,
              "totalresults": 1,
              "results": [
                {
                  "id": "sk-101",
                  "eventname": "Bicep Live",
                  "date": "2026-11-26",
                  "openingtimes": {
                    "doorsopen": "19:00"
                  },
                  "link": "https://www.skiddle.com/whats-on/London/Drumsheds/event1/",
                  "cancelled": false,
                  "tickets": true,
                  "venue": {
                    "name": "Drumsheds",
                    "town": "London"
                  }
                }
              ]
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("sk-101");
        ev.Name.ShouldBe("Bicep Live");
        ev.VenueName.ShouldBe("Drumsheds");
        ev.Date.ShouldBe(new DateOnly(2026, 11, 26));
        ev.Time.ShouldBe(new TimeOnly(19, 0, 0));
        ev.TicketUrl.ShouldBe("https://www.skiddle.com/whats-on/London/Drumsheds/event1/");
        ev.Status.ShouldBe(EventStatus.OnSale);
        ev.Provider.ShouldBe(EventProvider.Skiddle);
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenResultsOmittedOrEmpty()
    {
        const string json = """
            {
              "error": 0,
              "totalresults": 0,
              "results": []
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("NonExistentArtist");

        result.ShouldBeEmpty();
    }

    private static (SkiddleClient Client, CapturingHttpMessageHandler Handler) CreateClient(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}",
        string apiKey = "test-key"
    )
    {
        var options = Options.Create(new SkiddleOptions { ApiKey = apiKey });
        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.skiddle.com/api/v1/") };
        var client = new SkiddleClient(httpClient, options, NullLogger<SkiddleClient>.Instance);
        return (client, handler);
    }
}
