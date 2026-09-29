using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@example")]
    [InlineData("user space@example.com")]
    public async Task Should_ReturnInvalidEmail_WhenEmailIsInvalid(string? email)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.SubscribeAsync(email, "Bicep", "London");

        result.Status.ShouldBe(SubscribeStatus.InvalidEmail);
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnInvalidArtist_WhenArtistNameIsEmpty(string? artistName)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.SubscribeAsync("fan@electroniclive.com", artistName, "London");

        result.Status.ShouldBe(SubscribeStatus.InvalidArtist);
        result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnArtistNotVerified_WhenArtistVerificationFails()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        _artistVerificationService.VerifyArtistExistsAsync("FakeArtist", Arg.Any<CancellationToken>()).Returns(false);

        var result = await service.SubscribeAsync("fan@electroniclive.com", "FakeArtist", "London");

        result.Status.ShouldBe(SubscribeStatus.ArtistNotVerified);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("FakeArtist");
    }

    [Fact]
    public async Task Should_CreateNewUserAndSubscription_WhenValidDataProvided()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.SubscribeAsync("fan@electroniclive.com", "Bicep", null);

        result.Status.ShouldBe(SubscribeStatus.Created);
        result.SubscriptionId.ShouldNotBe(Guid.Empty);

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.ShouldNotBeNullOrWhiteSpace();
        user.UnsubscribeToken.Length.ShouldBe(64);
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(result.SubscriptionId);
        sub.ArtistName.ShouldBe("bicep");
        sub.City.ShouldBe("London");
        sub.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_ReturnAlreadySubscribed_WhenActiveSubscriptionExists()
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", true));

        using var context = CreateContext();
        var service = CreateService(context);
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.SubscribeAsync("fan@electroniclive.com", "Bicep", "London");

        result.Status.ShouldBe(SubscribeStatus.AlreadySubscribed);
        result.SubscriptionId.ShouldBe(subIds[0]);
    }

    [Fact]
    public async Task Should_ReactivateSubscription_WhenInactiveSubscriptionExists()
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", false));

        using var context = CreateContext();
        var service = CreateService(context);
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.SubscribeAsync("fan@electroniclive.com", "Bicep", "London");

        result.Status.ShouldBe(SubscribeStatus.Reactivated);
        result.SubscriptionId.ShouldBe(subIds[0]);

        using var verifyContext = CreateContext();
        var updated = await verifyContext.Subscriptions.FindAsync(subIds[0]);
        updated!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnMissingToken_WhenUnsubscribeTokenIsEmpty(string? token)
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync(token, null);

        result.Status.ShouldBe(UnsubscribeStatus.MissingToken);
    }

    [Fact]
    public async Task Should_ReturnInvalidToken_WhenUserNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync("non-existent", null);

        result.Status.ShouldBe(UnsubscribeStatus.InvalidToken);
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

        result.Status.ShouldBe(UnsubscribeStatus.AllSuccess);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnNotSubscribed_WhenArtistIsNotActive()
    {
        await SeedUser("fan@electroniclive.com", "token-inactive", ("bicep", "London", false));

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UnsubscribeAsync("token-inactive", "Bicep");

        result.Status.ShouldBe(UnsubscribeStatus.NotSubscribed);
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var service = CreateService(context);
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", cts.Token).Returns(true);

        await service.SubscribeAsync("fan@electroniclive.com", "Bicep", "London", cts.Token);

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
}
