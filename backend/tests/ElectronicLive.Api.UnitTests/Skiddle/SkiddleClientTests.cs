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
        query.ShouldContain("eventcode=LIVE,CLUB,FEST");
        query.ShouldContain("order=date");
        query.ShouldContain("description=1");
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

    [Fact]
    public async Task Should_OmitGeoCoordinates_AndFilterByVenueTown_WhenCityIsUnmapped()
    {
        const string json = """
            {
              "error": 0,
              "results": [
                {
                  "id": "1",
                  "eventname": "Bicep - Inverness Gig",
                  "venue": { "name": "Ironworks", "town": "Inverness" }
                },
                {
                  "id": "2",
                  "eventname": "Bicep - Edinburgh Gig",
                  "venue": { "name": "Liquid Room", "town": "Edinburgh" }
                }
              ]
            }
            """;

        var (client, handler) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep", "Inverness");

        handler.LastRequest.ShouldNotBeNull();
        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldNotContain("latitude=");
        query.ShouldNotContain("longitude=");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("1");
        ev.Name.ShouldBe("Bicep - Inverness Gig");
    }

    [Fact]
    public async Task Should_FilterOut_IrrelevantEvents_WhenQueryDoesNotMatchEventName_Venue_OrArtists()
    {
        const string json = """
            {
              "error": 0,
              "results": [
                {
                  "id": "1",
                  "eventname": "Get Wild Shoreditch Party - Everyone Free Before 12AM",
                  "venue": { "name": "The Lighthouse Bar And Club", "town": "London" },
                  "artists": []
                },
                {
                  "id": "2",
                  "eventname": "Absolute Amy - Amy Winehouse Tribute",
                  "venue": { "name": "Basing House", "town": "London" },
                  "artists": [
                    { "artistid": "10", "name": "Amy Winehouse" },
                    { "artistid": "11", "name": "Absolute Amy" }
                  ]
                },
                {
                  "id": "3",
                  "eventname": "Amy Wiles Presents Eternity",
                  "venue": { "name": "Village Underground", "town": "London" },
                  "artists": [
                    { "artistid": "20", "name": "Amy Wiles" }
                  ]
                },
                {
                  "id": "4",
                  "eventname": "Don't Let Daddy Know",
                  "venue": { "name": "Drumsheds", "town": "London" },
                  "artists": [
                    { "artistid": "30", "name": "R3hab" },
                    { "artistid": "20", "name": "Amy Wiles" }
                  ]
                }
              ]
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Amy Wiles");

        result.Count.ShouldBe(2);
        result.Select(e => e.Id).ShouldBe(["3", "4"]);
        result[0].Name.ShouldBe("Amy Wiles Presents Eternity");
        result[1].Name.ShouldBe("Don't Let Daddy Know");
    }

    [Fact]
    public async Task Should_MatchEvent_WhenQueryMatchesVenueName()
    {
        const string json = """
            {
              "error": 0,
              "results": [
                {
                  "id": "100",
                  "eventname": "Club Night",
                  "venue": { "name": "Village Underground", "town": "London" }
                }
              ]
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Village Underground");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("100");
    }

    [Fact]
    public async Task Should_MatchEvent_WhenMultiWordTokensAppearAcrossCompositeFields()
    {
        const string json = """
            {
              "error": 0,
              "results": [
                {
                  "id": "200",
                  "eventname": "Exhale London",
                  "venue": { "name": "Drumsheds", "town": "London" },
                  "artists": [
                    { "artistid": "50", "name": "Amelie Lens" }
                  ]
                }
              ]
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Amelie Lens Drumsheds");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("200");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultToLondon_WhenCityNullOrWhitespace(string? city)
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep", city!);

        handler.LastRequest.ShouldNotBeNull();
        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("latitude=51.5074");
        query.ShouldContain("longitude=-0.1278");
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenSkiddleReturnsHttp200ErrorPayload()
    {
        const string json = """
            {
              "error": 1,
              "description": "Invalid API key provided"
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep");

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
