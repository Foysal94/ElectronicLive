using System.Net;
using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.Ticketmaster;

public class TicketmasterClientTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("none")]
    [InlineData("NONE")]
    public async Task Should_ReturnEmpty_WhenApiKeyMissingOrNone(string? apiKey)
    {
        var (client, handler) = CreateClient(apiKey: apiKey!);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_ConstructExpectedRequestUrl()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep & Hammer", city: "London & South");

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
    public async Task Should_DefaultCityToLondon_WhenCityOmitted()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep");

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.ToString().ShouldContain("city=London");
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
    public async Task Should_ParseHalJson_AndMapPropertiesCorrectly()
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

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep");

        var ev = result.ShouldHaveSingleItem();
        ev.Id.ShouldBe("event-1");
        ev.Name.ShouldBe("Bicep Live");
        ev.VenueName.ShouldBe("Royal Albert Hall");
        ev.Date.ShouldBe(new DateOnly(2026, 11, 26));
        ev.Time.ShouldBe(new TimeOnly(18, 0, 0));
        ev.TicketUrl.ShouldBe("https://ticketmaster.co.uk/event1");
        ev.Status.ShouldBe(EventStatus.OnSale);
        ev.Provider.ShouldBe(EventProvider.Ticketmaster);
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenEmbeddedOmitted()
    {
        const string json = """
            {
              "page": {
                "size": 20,
                "totalElements": 0
              }
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("NonExistentArtist");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_FallbackToUnknownVenue_WhenVenuesEmpty()
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

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Secret");

        var ev = result.ShouldHaveSingleItem();
        ev.VenueName.ShouldBe("Unknown Venue");
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenHttpFails()
    {
        var (client, _) = CreateClient(statusCode: HttpStatusCode.InternalServerError);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenHttpRequestTimesOut()
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new CapturingHttpMessageHandler(_ => throw new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.ticketmaster.com/discovery/v2/"),
        };
        var client = new TicketmasterClient(httpClient, options, NullLogger<TicketmasterClient>.Instance);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ApplyMusicClassificationAndGenreKeyword_WhenGenreIsSpecified()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync(null, "drum-and-bass");

        handler.LastRequest.ShouldNotBeNull();
        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("keyword=Drum%20and%20Bass");
        query.ShouldContain("classificationName=music");
    }

    [Fact]
    public async Task Should_CombineQueryAndGenreKeyword_WhenBothSpecified()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep", "techno");

        handler.LastRequest.ShouldNotBeNull();
        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("keyword=Bicep%20Techno");
        query.ShouldContain("classificationName=music");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task Should_ReturnEmpty_WhenBothQueryAndGenreAreNullOrWhitespace(string? query, string? genre)
    {
        var (client, handler) = CreateClient();

        var result = await client.SearchEventsAsync(query, genre);

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnFalse_WhenArtistVerification_ArtistNameIsNullOrEmpty(string? artistName)
    {
        var (client, handler) = CreateClient();
        var result = await client.VerifyArtistExistsAsync(artistName!);

        result.ShouldBeFalse();
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("none")]
    [InlineData("NONE")]
    public async Task Should_ReturnFalse_WhenArtistVerification_ApiKeyMissingOrNone(string? apiKey)
    {
        var (client, handler) = CreateClient(apiKey: apiKey!);
        var result = await client.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_ConstructExpectedRequestUrl_WhenVerifyingArtist()
    {
        var (client, handler) = CreateClient(responseBody: AttractionJson("Bicep & Hammer"));
        var result = await client.VerifyArtistExistsAsync("Bicep & Hammer");

        result.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);

        var query = handler.LastRequest.RequestUri!.PathAndQuery;
        query.ShouldContain("attractions.json");
        query.ShouldContain("apikey=test-key");
        query.ShouldContain("keyword=Bicep%20%26%20Hammer");
        query.ShouldContain("classificationName=music");
    }

    [Fact]
    public async Task Should_ReturnTrue_WhenVerifyingArtist_AndAttractionNameMatches()
    {
        var (client, _) = CreateClient(responseBody: AttractionJson("Charlotte de Witte"));
        var result = await client.VerifyArtistExistsAsync("Charlotte de Witte");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenVerifyingArtist_AndAttractionsDoNotMatchArtistName()
    {
        var (client, _) = CreateClient(responseBody: AttractionJson("Completely Unrelated Artist"));
        var result = await client.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenVerifyingArtist_AndNoAttractionsFound()
    {
        var (client, _) = CreateClient(responseBody: AttractionJson(totalElements: 0));
        var result = await client.VerifyArtistExistsAsync("FakeArtist12345");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenVerifyingArtist_AndHttpCallFails()
    {
        var (client, _) = CreateClient(statusCode: HttpStatusCode.InternalServerError);
        var result = await client.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenVerifyingArtist_AndHttpRequestTimesOut()
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new CapturingHttpMessageHandler(_ => throw new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.ticketmaster.com/discovery/v2/"),
        };
        var client = new TicketmasterClient(httpClient, options, NullLogger<TicketmasterClient>.Instance);

        var result = await client.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ForwardCancellationToken_WhenVerifyingArtist()
    {
        var (client, handler) = CreateClient();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.VerifyArtistExistsAsync("Bicep", cancellationToken: cts.Token)
        );
        handler.WasCanceledDuringSend.ShouldBeTrue();
    }

    private static string AttractionJson(string? artistName = null, int totalElements = 1) =>
        artistName is null
            ? $"{{\"page\":{{\"totalElements\":{totalElements}}}}}"
            : $"{{\"_embedded\":{{\"attractions\":[{{\"id\":\"attr-1\",\"name\":\"{artistName}\"}}]}},\"page\":{{\"totalElements\":{totalElements}}}}}";

    private static (TicketmasterClient Client, CapturingHttpMessageHandler Handler) CreateClient(
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
        var client = new TicketmasterClient(httpClient, options, NullLogger<TicketmasterClient>.Instance);
        return (client, handler);
    }
}
