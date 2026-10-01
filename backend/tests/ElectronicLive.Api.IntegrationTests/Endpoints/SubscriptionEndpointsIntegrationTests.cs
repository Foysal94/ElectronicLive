using System.Net;
using System.Net.Http.Json;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.IntegrationTests.Infrastructure;
using ElectronicLive.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.IntegrationTests.Endpoints;

public class SubscriptionEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SubscriptionEndpointsIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task Should_CreateUserAndSubscription_AndReturnCreated_WhenSubscribingNewUser()
    {
        _factory.ArtistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var payload = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SubscribeResponse>();
        body.ShouldNotBeNull();
        body.SubscriptionId.ShouldNotBe(Guid.Empty);
        body.Message.ShouldBe("Subscribed successfully");
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldBe($"/api/subscriptions/{body.SubscriptionId}");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var user = await db
                .Users.Include(u => u.Subscriptions)
                .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
            user.ShouldNotBeNull();
            user.UnsubscribeToken.Length.ShouldBe(64);

            var sub = user.Subscriptions.ShouldHaveSingleItem();
            sub.Id.ShouldBe(body.SubscriptionId);
            sub.ArtistName.ShouldBe("bicep");
            sub.City.ShouldBe("London");
            sub.IsActive.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_ReturnOk_WhenSubscriptionAlreadyExists()
    {
        var existingSubId = Guid.NewGuid();
        await SeedUserWithSubscriptionAsync(
            "fan@electroniclive.com",
            "token123",
            existingSubId,
            "bicep",
            "London",
            isActive: true
        );

        _factory.ArtistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var payload = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscribeResponse>();
        body.ShouldNotBeNull();
        body.SubscriptionId.ShouldBe(existingSubId);
        body.Message.ShouldContain("Already subscribed to Bicep in London.");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var subs = await db.Subscriptions.Where(s => s.ArtistName == "bicep").ToListAsync();
            subs.Count.ShouldBe(1);
            subs[0].IsActive.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_ReactivateSubscription_AndReturnOk_WhenSubscribingToPreviouslyDeactivatedSubscription()
    {
        var deactivatedSubId = Guid.NewGuid();
        await SeedUserWithSubscriptionAsync(
            "fan@electroniclive.com",
            "token123",
            deactivatedSubId,
            "bicep",
            "London",
            isActive: false
        );

        _factory.ArtistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var payload = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscribeResponse>();
        body.ShouldNotBeNull();
        body.SubscriptionId.ShouldBe(deactivatedSubId);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(deactivatedSubId);
            sub.ShouldNotBeNull();
            sub.IsActive.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEmailIsInvalid()
    {
        var payload = new
        {
            email = "not-an-email",
            artistName = "Bicep",
            city = "London",
        };
        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("email");
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenArtistFailsVerification()
    {
        _factory
            .ArtistVerificationService.VerifyArtistExistsAsync("FakeArtistXYZ", Arg.Any<CancellationToken>())
            .Returns(false);

        var payload = new SubscribeRequest("fan@electroniclive.com", "FakeArtistXYZ", "London");
        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("artistName");
        problem.Errors["artistName"][0].ShouldContain("FakeArtistXYZ");
    }

    [Fact]
    public async Task Should_DeactivateSubscription_AndReturnOkHtml_WhenUnsubscribeWithValidTokenAndArtist()
    {
        var subId = Guid.NewGuid();
        const string token = "token-unsubscribe-test-1234567890123456789012345678901234567890";
        await SeedUserWithSubscriptionAsync("fan@electroniclive.com", token, subId, "bicep", "London", isActive: true);

        var response = await _client.GetAsync($"/api/subscriptions/unsubscribe?token={token}&artist=bicep");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("unsubscribed from alerts for bicep");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var sub = await db.Subscriptions.FindAsync(subId);
            sub.ShouldNotBeNull();
            sub.IsActive.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_DeactivateAllSubscriptions_WhenArtistParameterIsOmitted()
    {
        var sub1Id = Guid.NewGuid();
        var sub2Id = Guid.NewGuid();
        const string token = "token-global-unsubscribe-1234567890123456789012345678901234567890";

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "fan@electroniclive.com",
                UnsubscribeToken = token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.Subscriptions.Add(
                new Subscription
                {
                    Id = sub1Id,
                    UserId = user.Id,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            user.Subscriptions.Add(
                new Subscription
                {
                    Id = sub2Id,
                    UserId = user.Id,
                    ArtistName = "overmono",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });

        var response = await _client.GetAsync($"/api/subscriptions/unsubscribe?token={token}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("unsubscribed from all artist alerts");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var sub1 = await db.Subscriptions.FindAsync(sub1Id);
            var sub2 = await db.Subscriptions.FindAsync(sub2Id);
            sub1!.IsActive.ShouldBeFalse();
            sub2!.IsActive.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_ReturnNotFoundHtml_WhenUnsubscribeTokenIsInvalid()
    {
        var response = await _client.GetAsync("/api/subscriptions/unsubscribe?token=non-existent-token&artist=bicep");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Invalid or expired unsubscribe link");
    }

    [Fact]
    public async Task Should_ReturnOkHtml_WhenUnsubscribingFromArtistUserIsNotSubscribedTo_PurelyIdempotent()
    {
        var subId = Guid.NewGuid();
        const string token = "token-artist-diff-1234567890123456789012345678901234567890";
        await SeedUserWithSubscriptionAsync("fan@electroniclive.com", token, subId, "bicep", "London", isActive: true);

        var response = await _client.GetAsync($"/api/subscriptions/unsubscribe?token={token}&artist=overmono");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("unsubscribed from alerts for overmono");
    }

    [Fact]
    public async Task Should_ReturnBadRequestHtml_WhenUnsubscribeTokenIsMissing()
    {
        var response = await _client.GetAsync("/api/subscriptions/unsubscribe?artist=bicep");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("An unsubscribe token is required");
    }

    private async Task SeedUserWithSubscriptionAsync(
        string email,
        string token,
        Guid subscriptionId,
        string artist,
        string city,
        bool isActive
    )
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                UnsubscribeToken = token,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.Subscriptions.Add(
                new Subscription
                {
                    Id = subscriptionId,
                    UserId = user.Id,
                    ArtistName = artist,
                    City = city,
                    IsActive = isActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });
    }
}
