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

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("email");
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

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("artistName");
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenArtistVerificationFails()
    {
        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "asdf123xyz", "London");
        _artistVerificationService.VerifyArtistExistsAsync("asdf123xyz", Arg.Any<CancellationToken>()).Returns(false);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var validationProblem = result.Result.ShouldBeOfType<ValidationProblem>();
        validationProblem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Errors.ShouldContainKey("artistName");
        validationProblem.ProblemDetails.Errors["artistName"][0].ShouldContain("asdf123xyz");
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
        createdResult.Value.ShouldNotBeNull();

        using var verifyContext = CreateContext();
        var sub = await verifyContext.Subscriptions.FirstOrDefaultAsync(s =>
            s.Id == createdResult.Value.SubscriptionId
        );
        sub.ShouldNotBeNull();
        sub.City.ShouldBe("Manchester");
    }

    [Fact]
    public async Task Should_ReturnOk_WhenSubscribingDuplicateActiveSubscription()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "token123",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            seedContext.Subscriptions.Add(
                new Subscription
                {
                    Id = subId,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var okResult = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldNotBeNull();
        okResult.Value.SubscriptionId.ShouldBe(subId);
        okResult.Value.Message.ShouldContain("Already subscribed");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenReactivatingInactiveSubscription()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "token123",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            seedContext.Subscriptions.Add(
                new Subscription
                {
                    Id = subId,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await SubscriptionEndpoints.Subscribe(request, context, _artistVerificationService);

        var okResult = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldNotBeNull();
        okResult.Value.SubscriptionId.ShouldBe(subId);
        okResult.Value.Message.ShouldContain("reactivated");

        using var verifyContext = CreateContext();
        var updatedSub = await verifyContext.Subscriptions.FindAsync(subId);
        updatedSub.ShouldNotBeNull();
        updatedSub.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Return400BadRequestHtml_WhenUnsubscribeTokenIsMissingOrEmpty(string? token)
    {
        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe(token, null, context);

        result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain("Invalid unsubscribe request");
    }

    [Fact]
    public async Task Should_Return404NotFoundHtml_WhenUnsubscribeTokenIsInvalidOrNotFound()
    {
        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("non-existent-token", null, context);

        result.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain("Invalid or expired unsubscribe link");
    }

    [Fact]
    public async Task Should_DeactivateSpecificSubscription_WhenTokenAndArtistAreValid()
    {
        var userId = Guid.NewGuid();
        var sub1Id = Guid.NewGuid();
        var sub2Id = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "valid-token-123",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            seedContext.Subscriptions.AddRange(
                new Subscription
                {
                    Id = sub1Id,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new Subscription
                {
                    Id = sub2Id,
                    UserId = userId,
                    ArtistName = "charlotte de witte",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-123", "Bicep", context);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain("unsubscribed from alerts for Bicep");

        using var verifyContext = CreateContext();
        var verifiedSub1 = await verifyContext.Subscriptions.FindAsync(sub1Id);
        var verifiedSub2 = await verifyContext.Subscriptions.FindAsync(sub2Id);
        verifiedSub1.ShouldNotBeNull();
        verifiedSub1.IsActive.ShouldBeFalse();
        verifiedSub2.ShouldNotBeNull();
        verifiedSub2.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_DeactivateAllSubscriptions_WhenTokenIsValidAndArtistIsOmitted()
    {
        var userId = Guid.NewGuid();
        var sub1Id = Guid.NewGuid();
        var sub2Id = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "valid-token-all",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            seedContext.Subscriptions.AddRange(
                new Subscription
                {
                    Id = sub1Id,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new Subscription
                {
                    Id = sub2Id,
                    UserId = userId,
                    ArtistName = "overmono",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-all", null, context);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain("unsubscribed from all artist alerts");

        using var verifyContext = CreateContext();
        var verifiedSub1 = await verifyContext.Subscriptions.FindAsync(sub1Id);
        var verifiedSub2 = await verifyContext.Subscriptions.FindAsync(sub2Id);
        verifiedSub1!.IsActive.ShouldBeFalse();
        verifiedSub2!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_DeactivateAllCitySubscriptions_WhenUnsubscribingByArtistWithMultipleCities()
    {
        var userId = Guid.NewGuid();
        var subLondonId = Guid.NewGuid();
        var subMcrId = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "valid-token-cities",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            seedContext.Subscriptions.AddRange(
                new Subscription
                {
                    Id = subLondonId,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "London",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new Subscription
                {
                    Id = subMcrId,
                    UserId = userId,
                    ArtistName = "bicep",
                    City = "Manchester",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-cities", "Bicep", context);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);

        using var verifyContext = CreateContext();
        var londonSub = await verifyContext.Subscriptions.FindAsync(subLondonId);
        var mcrSub = await verifyContext.Subscriptions.FindAsync(subMcrId);
        londonSub!.IsActive.ShouldBeFalse();
        mcrSub!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnHtmlSuccess_WhenUnsubscribingArtistUserIsNotSubscribedTo()
    {
        var userId = Guid.NewGuid();
        using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(
                new User
                {
                    Id = userId,
                    Email = "fan@electroniclive.com",
                    UnsubscribeToken = "valid-token-not-subbed",
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            );
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token-not-subbed", "Fred Again..", context);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain("not currently subscribed");
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
}
