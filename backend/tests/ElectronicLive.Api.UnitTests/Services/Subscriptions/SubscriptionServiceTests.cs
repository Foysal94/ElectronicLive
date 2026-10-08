using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using ElectronicLive.Api.Services.Subscriptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.UnitTests.Services.Subscriptions;

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

    [Fact]
    public async Task Should_ReturnNull_WhenArtistNameIsEmpty()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "", "London");

        var outcome = await service.SubscribeAsync(request);

        outcome.ShouldBeNull();
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnNull_WhenArtistVerificationFails()
    {
        _artistVerificationService.VerifyArtistExistsAsync("FakeArtist", Arg.Any<CancellationToken>()).Returns(false);

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "FakeArtist", "London");

        var outcome = await service.SubscribeAsync(request);

        outcome.ShouldBeNull();
    }

    [Fact]
    public async Task Should_CreateNewUserAndSubscription_WhenSubscribingNewUser()
    {
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", null);

        var outcome = await service.SubscribeAsync(request);

        outcome.ShouldNotBeNull();
        outcome.IsNew.ShouldBeTrue();
        outcome.SubscriptionId.ShouldNotBe(Guid.Empty);

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.Length.ShouldBe(64);
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(outcome.SubscriptionId);
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

        var outcome = await service.SubscribeAsync(request);

        outcome.ShouldNotBeNull();
        outcome.IsNew.ShouldBeFalse();
        outcome.SubscriptionId.ShouldBe(subIds[0]);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, UnsubscribeOutcome.MissingToken)]
    [InlineData("", UnsubscribeOutcome.MissingToken)]
    [InlineData("   ", UnsubscribeOutcome.MissingToken)]
    [InlineData("non-existent-token", UnsubscribeOutcome.InvalidToken)]
    public async Task Should_ReturnErrorOutcome_WhenTokenIsMissingOrNotFound(
        string? token,
        UnsubscribeOutcome expectedOutcome
    )
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var outcome = await service.UnsubscribeAsync(token, artist: null);

        outcome.ShouldBe(expectedOutcome);
    }

    [Fact]
    public async Task Should_DeactivateMatchingArtistSubscriptions_AcrossCities()
    {
        var (_, subIds) = await SeedUser(
            "fan@electroniclive.com",
            "token-artist",
            (Artist: "bicep", City: "London", IsActive: true),
            (Artist: "bicep", City: "Manchester", IsActive: true),
            (Artist: "overmono", City: "London", IsActive: true)
        );

        using var context = CreateContext();
        var service = CreateService(context);

        var outcome = await service.UnsubscribeAsync("token-artist", "Bicep");

        outcome.ShouldBe(UnsubscribeOutcome.Success);

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
            (Artist: "bicep", City: "London", IsActive: true),
            (Artist: "overmono", City: "London", IsActive: true)
        );

        using var context = CreateContext();
        var service = CreateService(context);

        var outcome = await service.UnsubscribeAsync("token-all", artist: null);

        outcome.ShouldBe(UnsubscribeOutcome.Success);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnSuccess_WhenArtistIsNotActive_PurelyIdempotent()
    {
        await SeedUser("fan@electroniclive.com", "token-inactive", (Artist: "bicep", City: "London", IsActive: false));

        using var context = CreateContext();
        var service = CreateService(context);

        var outcome = await service.UnsubscribeAsync("token-inactive", "Bicep");

        outcome.ShouldBe(UnsubscribeOutcome.Success);
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
