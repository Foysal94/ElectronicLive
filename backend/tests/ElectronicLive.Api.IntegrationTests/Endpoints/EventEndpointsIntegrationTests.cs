using System.Net;
using System.Net.Http.Json;
using ElectronicLive.Api.Exceptions;
using ElectronicLive.Api.IntegrationTests.Infrastructure;
using ElectronicLive.Api.Models;
using Microsoft.AspNetCore.Http;

namespace ElectronicLive.Api.IntegrationTests.Endpoints;

public class EventEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EventEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _factory.ResetMocks();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_ReturnHealthyStatus_WhenCallingHealthEndpoint()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("healthy");
    }

    [Fact]
    public async Task Should_ReturnEvents_WhenSearchingByArtistQuery()
    {
        var expectedEvents = new List<EventResponse>
        {
            new(
                Id: "tm-1",
                Name: "Bicep Live",
                VenueName: "Alexandra Palace",
                Date: new DateOnly(2026, 11, 20),
                Time: new TimeOnly(20, 0),
                TicketUrl: "https://example.com/tickets",
                Status: EventStatus.OnSale,
                Provider: EventProvider.Ticketmaster
            ),
        };

        _factory
            .EventSearchService.SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
            .Returns(expectedEvents);

        var response = await _client.GetAsync("/api/events/search?query=Bicep");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        body.ShouldNotBeNull();
        var item = body.ShouldHaveSingleItem();
        item.Id.ShouldBe("tm-1");
        item.Name.ShouldBe("Bicep Live");
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNeitherQueryNorGenreIsProvided()
    {
        var response = await _client.GetAsync("/api/events/search");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("query");
    }

    [Fact]
    public async Task Should_ReturnBadGateway_WhenAllProvidersAreUnavailable()
    {
        _factory
            .EventSearchService.SearchEventsAsync("Bicep", null, "London", Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<IReadOnlyList<EventResponse>>(new AllProvidersUnavailableException("Bicep", 3))
            );

        var response = await _client.GetAsync("/api/events/search?query=Bicep");

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
    }

    private sealed record HealthResponse(string Status);
}
