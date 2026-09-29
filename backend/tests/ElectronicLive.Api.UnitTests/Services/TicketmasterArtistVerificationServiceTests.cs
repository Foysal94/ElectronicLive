using System.Net;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Services;
using ElectronicLive.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.Services;

public class TicketmasterArtistVerificationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnFalse_WhenArtistNameIsNullOrEmpty(string? artistName)
    {
        var (service, handler) = CreateService();

        var result = await service.VerifyArtistExistsAsync(artistName!);

        result.ShouldBeFalse();
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("none")]
    [InlineData("NONE")]
    public async Task Should_ReturnFalse_WhenApiKeyMissingOrNone(string? apiKey)
    {
        var (service, handler) = CreateService(apiKey: apiKey!);

        var result = await service.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_ConstructExpectedRequestUrl()
    {
        const string json = """
            {
              "_embedded": {
                "attractions": [
                  { "id": "attr-1", "name": "Bicep" }
                ]
              },
              "page": { "totalElements": 1 }
            }
            """;
        var (service, handler) = CreateService(responseBody: json);

        var result = await service.VerifyArtistExistsAsync("Bicep & Hammer");

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
    public async Task Should_ReturnTrue_WhenAttractionsFoundInEmbedded()
    {
        const string json = """
            {
              "_embedded": {
                "attractions": [
                  { "id": "attr-1", "name": "Charlotte de Witte" }
                ]
              },
              "page": { "totalElements": 1 }
            }
            """;
        var (service, _) = CreateService(responseBody: json);

        var result = await service.VerifyArtistExistsAsync("Charlotte de Witte");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenAttractionsDoNotMatchArtistName()
    {
        const string json = """
            {
              "_embedded": {
                "attractions": [
                  { "id": "attr-1", "name": "Completely Unrelated Artist" }
                ]
              },
              "page": { "totalElements": 1 }
            }
            """;
        var (service, _) = CreateService(responseBody: json);

        var result = await service.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenNoAttractionsFound()
    {
        const string json = """
            {
              "page": { "totalElements": 0 }
            }
            """;
        var (service, _) = CreateService(responseBody: json);

        var result = await service.VerifyArtistExistsAsync("FakeArtist12345");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenHttpCallFails()
    {
        var (service, _) = CreateService(statusCode: HttpStatusCode.InternalServerError);

        var result = await service.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_WhenHttpRequestTimesOut()
    {
        var options = Options.Create(new TicketmasterOptions { ApiKey = "test-key" });
        var handler = new CapturingHttpMessageHandler(_ => throw new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.ticketmaster.com/discovery/v2/"),
        };
        var service = new TicketmasterArtistVerificationService(
            httpClient,
            options,
            NullLogger<TicketmasterArtistVerificationService>.Instance
        );

        var result = await service.VerifyArtistExistsAsync("Bicep");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ForwardCancellationToken()
    {
        var (service, handler) = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.VerifyArtistExistsAsync("Bicep", cancellationToken: cts.Token)
        );
        handler.WasCanceledDuringSend.ShouldBeTrue();
    }

    private static (TicketmasterArtistVerificationService Service, CapturingHttpMessageHandler Handler) CreateService(
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
        var service = new TicketmasterArtistVerificationService(
            httpClient,
            options,
            NullLogger<TicketmasterArtistVerificationService>.Instance
        );
        return (service, handler);
    }
}
