using System.Net;
using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.ResidentAdvisor;

public class ResidentAdvisorClientTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnEmpty_WhenQueryNullOrWhitespace(string? query)
    {
        var (client, handler) = CreateClient();

        var result = await client.SearchEventsAsync(query!);

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_PostToGraphQLEndpoint_WithSearchQueryAndLimit()
    {
        var (client, handler) = CreateClient();

        await client.SearchEventsAsync("Bicep");

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://ra.co/graphql");

        var requestBody = await handler.LastRequest.Content!.ReadAsStringAsync();
        requestBody.ShouldContain("Bicep");
        requestBody.ShouldContain("SearchEvents");
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
    public async Task Should_IsolateUpstreamTimeout_AndReturnEmpty()
    {
        var options = Options.Create(new ResidentAdvisorOptions());
        var handler = new CapturingHttpMessageHandler(_ =>
            throw new TaskCanceledException("Upstream timeout simulation")
        );
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://ra.co/") };
        var client = new ResidentAdvisorClient(httpClient, options, NullLogger<ResidentAdvisorClient>.Instance);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_IsolateGeneralException_AndReturnEmpty()
    {
        var options = Options.Create(new ResidentAdvisorOptions());
        var handler = new CapturingHttpMessageHandler(_ => throw new HttpRequestException("Network failure"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://ra.co/") };
        var client = new ResidentAdvisorClient(httpClient, options, NullLogger<ResidentAdvisorClient>.Instance);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenNonSuccessStatusCode()
    {
        var (client, _) = CreateClient(statusCode: HttpStatusCode.BadGateway);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnEmpty_WhenGraphQLReturnsErrors()
    {
        const string json = """
            {
              "data": null,
              "errors": [
                { "message": "Field 'search' does not exist" }
              ]
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_FilterOutEvents_NotMatchingCity_CaseInsensitively()
    {
        const string json = """
            {
              "data": {
                "search": [
                  { "id": "1", "areaName": "Melbourne" },
                  { "id": "2", "areaName": "London" }
                ]
              }
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep", "london");

        result.ShouldHaveSingleItem().Id.ShouldBe("2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_DefaultToLondon_WhenCityNullOrWhitespace(string? city)
    {
        const string json = """
            {
              "data": {
                "search": [
                  { "id": "1", "areaName": "London" },
                  { "id": "2", "areaName": "Berlin" }
                ]
              }
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep", city!);

        result.ShouldHaveSingleItem().Id.ShouldBe("1");
    }

    [Fact]
    public async Task Should_FilterOutPastEvents_WhenSearchTypeIsPastEvent_OrDateInPast()
    {
        const string json = """
            {
              "data": {
                "search": [
                  { "id": "1", "areaName": "London", "searchType": "PASTEVENT", "date": "2030-01-01" },
                  { "id": "2", "areaName": "London", "searchType": "UPCOMINGEVENT", "date": "2020-01-01" },
                  { "id": "3", "areaName": "London", "searchType": "UPCOMINGEVENT", "date": "2030-01-01" }
                ]
              }
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep", "London");

        result.ShouldHaveSingleItem().Id.ShouldBe("3");
    }

    [Fact]
    public async Task Should_FilterOutEvents_WhenCountryIsNotUnitedKingdom()
    {
        const string json = """
            {
              "data": {
                "search": [
                  { "id": "1", "areaName": "London", "countryName": "Canada" },
                  { "id": "2", "areaName": "London", "countryName": "United Kingdom" }
                ]
              }
            }
            """;

        var (client, _) = CreateClient(responseBody: json);

        var result = await client.SearchEventsAsync("Bicep", "London");

        result.ShouldHaveSingleItem().Id.ShouldBe("2");
    }

    private static (ResidentAdvisorClient Client, CapturingHttpMessageHandler Handler) CreateClient(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{\"data\":{\"search\":[]}}",
        int limit = 20
    )
    {
        var options = Options.Create(new ResidentAdvisorOptions { Limit = limit });
        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://ra.co/") };
        var client = new ResidentAdvisorClient(httpClient, options, NullLogger<ResidentAdvisorClient>.Instance);
        return (client, handler);
    }
}
