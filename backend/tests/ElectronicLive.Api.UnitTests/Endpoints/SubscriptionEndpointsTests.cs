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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@example")]
    [InlineData("user space@example.com")]
    public async Task Should_ReturnValidationProblem_WhenEmailIsInvalid(string? email)
    {
        using var context = CreateContext();
        var request = new SubscribeRequest(email, "Bicep", "London");

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey("email");
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnValidationProblem_WhenArtistNameIsEmpty(string? artistName)
    {
        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", artistName, "London");

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey("artistName");
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

        var createdResult = result.Result.ShouldBeOfType<Created<SubscribeResponse>>();
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        createdResult.Value.ShouldNotBeNull();
        createdResult.Value.SubscriptionId.ShouldNotBe(Guid.Empty);
        createdResult.Value.Message.ShouldBe("Subscribed successfully");

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.ShouldNotBeNullOrWhiteSpace();
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(createdResult.Value.SubscriptionId);
        sub.ArtistName.ShouldBe("bicep");
        sub.City.ShouldBe("London");
        sub.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ReturnCreated_WithSpecifiedCity_WhenCityIsProvided()
    {
        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Four Tet", "Manchester");
        _artistVerificationService.VerifyArtistExistsAsync("Four Tet", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var createdResult = result.Result.ShouldBeOfType<Created<SubscribeResponse>>();
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);

        using var verifyContext = CreateContext();
        var sub = await verifyContext.Subscriptions.FirstOrDefaultAsync(s =>
            s.Id == createdResult.Value!.SubscriptionId
        );
        sub.ShouldNotBeNull();
        sub.City.ShouldBe("Manchester");
    }

    [Fact]
    public async Task Should_ReturnOk_WhenSubscribingDuplicateActiveSubscription()
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", true));

        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var okResult = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value!.SubscriptionId.ShouldBe(subIds[0]);
        okResult.Value.Message.ShouldContain("Already subscribed");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenReactivatingInactiveSubscription()
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", false));

        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var okResult = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value!.SubscriptionId.ShouldBe(subIds[0]);
        okResult.Value.Message.ShouldContain("reactivated");

        using var verifyContext = CreateContext();
        var updatedSub = await verifyContext.Subscriptions.FindAsync(subIds[0]);
        updatedSub!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Return400BadRequestHtml_WhenUnsubscribeTokenIsMissingOrEmpty(string? token)
    {
        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe(token, null, context);
        AssertHtml(result, StatusCodes.Status400BadRequest, "Invalid unsubscribe request");
    }

    [Fact]
    public async Task Should_Return404NotFoundHtml_WhenUnsubscribeTokenIsInvalidOrNotFound()
    {
        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe("non-existent-token", null, context);
        AssertHtml(result, StatusCodes.Status404NotFound, "Invalid or expired unsubscribe link");
    }

    [Fact]
    public async Task Should_DeactivateSpecificSubscription_WhenTokenAndArtistAreValid()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "valid-token-123",
            ("bicep", "London", true),
            ("charlotte de witte", "London", true)
        );

        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-123", "Bicep", context);
        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from alerts for Bicep");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_DeactivateAllSubscriptions_WhenTokenIsValidAndArtistIsOmitted()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "valid-token-all",
            ("bicep", "London", true),
            ("overmono", "London", true)
        );

        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-all", null, context);
        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from all artist alerts");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_DeactivateAllCitySubscriptions_WhenUnsubscribingByArtistWithMultipleCities()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "valid-token-cities",
            ("bicep", "London", true),
            ("bicep", "Manchester", true)
        );

        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-cities", "Bicep", context);
        result.StatusCode.ShouldBe(StatusCodes.Status200OK);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnHtmlSuccess_WhenUnsubscribingArtistUserIsNotSubscribedTo()
    {
        await SeedUser("fan@electroniclive.com", "valid-token-not-subbed");

        using var context = CreateContext();
        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-not-subbed", "Fred Again..", context);
        AssertHtml(result, StatusCodes.Status200OK, "not currently subscribed");
    }

    [Fact]
    public async Task Should_PropagateCancellationToken_OnSubscribe()
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
