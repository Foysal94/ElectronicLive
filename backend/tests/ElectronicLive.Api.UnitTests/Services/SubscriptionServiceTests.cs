using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.UnitTests.Services;

public sealed class SubscriptionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ElectronicLiveDbContext> _options;
    private readonly IArtistVerificationService _artistVerificationService =
        Substitute.For<IArtistVerificationService>();

    public SubscriptionServiceTests()
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

    private SubscriptionService CreateService(ElectronicLiveDbContext context) =>
        new(context, _artistVerificationService);

    [Theory]
    [InlineData(null, "Bicep", "email")]
    [InlineData("", "Bicep", "email")]
    [InlineData("   ", "Bicep", "email")]
    [InlineData("invalid-email", "Bicep", "email")]
    [InlineData("fan@electroniclive.com", null, "artistName")]
    [InlineData("fan@electroniclive.com", "", "artistName")]
    public async Task Should_ReturnInvalidInput_WhenRequestFailsValidation(
        string? email,
        string? artistName,
        string expectedErrorKey
    )
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest(email, artistName, "London");

        var result = await service.SubscribeAsync(request);

        result.Status.ShouldBe(SubscribeStatus.InvalidInput);
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContainKey(expectedErrorKey);
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnArtistNotFound_WhenArtistVerificationFails()
    {
        _artistVerificationService.VerifyArtistExistsAsync("FakeArtist", Arg.Any<CancellationToken>()).Returns(false);

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "FakeArtist", "London");

        var result = await service.SubscribeAsync(request);

        result.Status.ShouldBe(SubscribeStatus.ArtistNotFound);
        result.Message.ShouldContain("FakeArtist");
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContainKey("artistName");
    }

    [Fact]
    public async Task Should_CreateNewUserAndSubscription_WhenSubscribingNewUser()
    {
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", null);

        var result = await service.SubscribeAsync(request);

        result.Status.ShouldBe(SubscribeStatus.Created);
        result.SubscriptionId.ShouldNotBeNull();
        result.SubscriptionId.Value.ShouldNotBe(Guid.Empty);
        result.Message.ShouldBe("Subscribed successfully");

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.Length.ShouldBe(64);
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(result.SubscriptionId.Value);
        sub.ArtistName.ShouldBe("bicep");
        sub.City.ShouldBe("London");
        sub.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_HandleExistingSubscriptionLifecycle(bool initiallyActive)
    {
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", initiallyActive));

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");

        var result = await service.SubscribeAsync(request);

        result.Status.ShouldBe(SubscribeStatus.AlreadySubscribed);
        result.SubscriptionId.ShouldBe(subIds[0]);
        result.Message.ShouldContain("Already subscribed to Bicep in London.");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, UnsubscribeStatus.MissingToken, "An unsubscribe token is required.")]
    [InlineData("", UnsubscribeStatus.MissingToken, "An unsubscribe token is required.")]
    [InlineData("   ", UnsubscribeStatus.MissingToken, "An unsubscribe token is required.")]
    [InlineData("non-existent-token", UnsubscribeStatus.InvalidToken, "Invalid or expired unsubscribe link.")]
    public async Task Should_ReturnError_WhenTokenIsMissingOrNotFound(
        string? token,
        UnsubscribeStatus expectedStatus,
        string expectedSnippet
    )
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync(token, null);

        result.Status.ShouldBe(expectedStatus);
        result.Message.ShouldContain(expectedSnippet);
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
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync("token-artist", "Bicep");

        result.Status.ShouldBe(UnsubscribeStatus.Success);
        result.Message.ShouldContain("unsubscribed from alerts for Bicep");

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
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync("token-all", null);

        result.Status.ShouldBe(UnsubscribeStatus.Success);
        result.Message.ShouldContain("unsubscribed from all artist alerts");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnSuccessMessage_WhenArtistIsNotActive_PurelyIdempotent()
    {
        await SeedUser("fan@electroniclive.com", "token-inactive", ("bicep", "London", false));

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync("token-inactive", "Bicep");

        result.Status.ShouldBe(UnsubscribeStatus.Success);
        result.Message.ShouldContain("unsubscribed from alerts for Bicep");
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
}
