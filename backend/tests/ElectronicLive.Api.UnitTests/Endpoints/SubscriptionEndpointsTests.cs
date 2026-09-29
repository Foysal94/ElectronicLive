using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.UnitTests.Endpoints;

public sealed class SubscriptionEndpointsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ElectronicLiveDbContext> _options;
    private readonly IArtistVerificationService _artistVerificationService =
        Substitute.For<IArtistVerificationService>();

    public SubscriptionEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        _options = new DbContextOptionsBuilder<ElectronicLiveDbContext>().UseSqlite(_connection).Options;

        using var context = new ElectronicLiveDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private ElectronicLiveDbContext CreateContext() => new(_options);

    [Theory]
    [InlineData(null, "Bicep", "email")]
    [InlineData("", "Bicep", "email")]
    [InlineData("   ", "Bicep", "email")]
    [InlineData("invalid-email", "Bicep", "email")]
    [InlineData("user@", "Bicep", "email")]
    [InlineData("fan@electroniclive.com", null, "artistName")]
    [InlineData("fan@electroniclive.com", "", "artistName")]
    [InlineData("fan@electroniclive.com", "   ", "artistName")]
    public async Task Should_ReturnValidationProblem_WhenInputIsInvalid(
        string? email,
        string? artistName,
        string expectedErrorKey
    )
    {
        using var context = CreateContext();
        var request = new SubscribeRequest(email, artistName, "London");

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey(expectedErrorKey);
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenArtistVerificationFails()
    {
        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "asdf123xyz", "London");
        _artistVerificationService.VerifyArtistExistsAsync("asdf123xyz", Arg.Any<CancellationToken>()).Returns(false);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey("artistName");
        problem.ProblemDetails.Errors["artistName"][0].ShouldContain("asdf123xyz");
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenSubscribingNewUserAndArtist()
    {
        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", null);
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var created = result.Result.ShouldBeOfType<Created<SubscribeResponse>>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        created.Value!.SubscriptionId.ShouldNotBe(Guid.Empty);
        created.Location.ShouldBe($"/api/subscriptions/{created.Value.SubscriptionId}");

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.Length.ShouldBe(64);
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(created.Value.SubscriptionId);
        sub.ArtistName.ShouldBe("bicep");
        sub.City.ShouldBe("London");
        sub.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, StatusCodes.Status200OK, "Already subscribed")]
    [InlineData(false, StatusCodes.Status200OK, "reactivated")]
    public async Task Should_HandleExistingSubscriptionLifecycle(
        bool initiallyActive,
        int expectedStatusCode,
        string messageSubstring
    )
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", initiallyActive));

        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var ok = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        ok.StatusCode.ShouldBe(expectedStatusCode);
        ok.Value!.SubscriptionId.ShouldBe(subIds[0]);
        ok.Value.Message.ShouldContain(messageSubstring);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, StatusCodes.Status400BadRequest, "An unsubscribe token is required.")]
    [InlineData("", StatusCodes.Status400BadRequest, "An unsubscribe token is required.")]
    [InlineData("   ", StatusCodes.Status400BadRequest, "An unsubscribe token is required.")]
    [InlineData("non-existent-token", StatusCodes.Status404NotFound, "Invalid or expired unsubscribe link.")]
    public async Task Should_ReturnErrorHtml_WhenTokenIsMissingOrNotFound(
        string? token,
        int expectedStatusCode,
        string expectedSnippet
    )
    {
        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe(token, null, context);

        AssertHtml(result, expectedStatusCode, expectedSnippet);
    }

    [Fact]
    public async Task Should_DeactivateMatchingArtistSubscriptions_AcrossCities()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "token-artist",
            ("bicep", "London", true),
            ("bicep", "Manchester", true),
            ("overmono", "London", true)
        );

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("token-artist", "Bicep", context);

        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from alerts for Bicep");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[2]))!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_DeactivateAllSubscriptions_WhenArtistIsOmitted()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "token-all",
            ("bicep", "London", true),
            ("overmono", "London", true)
        );

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("token-all", null, context);

        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from all artist alerts");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnNotSubscribedHtml_WhenArtistIsNotActive()
    {
        await SeedUser("fan@electroniclive.com", "token-inactive", ("bicep", "London", false));

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("token-inactive", "Bicep", context);

        AssertHtml(result, StatusCodes.Status200OK, "not currently subscribed");
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", cts.Token).Returns(true);

        await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService, cts.Token);

        await _artistVerificationService.Received(1).VerifyArtistExistsAsync("Bicep", cts.Token);
    }

    private async Task<(Guid UserId, List<Guid> SubIds)> SeedUser(
        string email,
        string token,
        params (string Artist, string City, bool IsActive)[] subscriptions
    )
    {
        using var context = CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UnsubscribeToken = token,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.Users.Add(user);

        var subIds = new List<Guid>(subscriptions.Length);
        foreach (var (artist, city, isActive) in subscriptions)
        {
            var subId = Guid.NewGuid();
            subIds.Add(subId);
            context.Subscriptions.Add(
                new Subscription
                {
                    Id = subId,
                    UserId = user.Id,
                    ArtistName = artist,
                    City = city,
                    IsActive = isActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
        }

        await context.SaveChangesAsync();
        return (user.Id, subIds);
    }

    private static void AssertHtml(ContentHttpResult result, int expectedStatusCode, string expectedSnippet)
    {
        result.StatusCode.ShouldBe(expectedStatusCode);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain(expectedSnippet);
    }
}
