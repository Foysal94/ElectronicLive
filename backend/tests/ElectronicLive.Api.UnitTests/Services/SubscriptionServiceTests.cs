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

    private static SubscriptionService CreateService(ElectronicLiveDbContext context) => new(context);

    [Fact]
    public async Task Should_CreateNewUserAndSubscription_WhenSubscribingNewUser()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", null);

        var (subscriptionId, isNew) = await service.SubscribeAsync(request);

        isNew.ShouldBeTrue();
        subscriptionId.ShouldNotBe(Guid.Empty);

        using var verifyContext = CreateContext();
        var user = await verifyContext
            .Users.Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Email == "fan@electroniclive.com");
        user.ShouldNotBeNull();
        user.UnsubscribeToken.Length.ShouldBe(64);
        var sub = user.Subscriptions.ShouldHaveSingleItem();
        sub.Id.ShouldBe(subscriptionId);
        sub.ArtistName.ShouldBe("bicep");
        sub.City.ShouldBe("London");
        sub.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Should_HandleExistingSubscriptionLifecycle(bool initiallyActive, bool expectedIsNew)
    {
        var (_, subIds) = await SeedUser("fan@electroniclive.com", "token123", ("bicep", "London", initiallyActive));

        using var context = CreateContext();
        var service = CreateService(context);
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");

        var (subscriptionId, isNew) = await service.SubscribeAsync(request);

        isNew.ShouldBe(expectedIsNew);
        subscriptionId.ShouldBe(subIds[0]);

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, false, "An unsubscribe token is required.")]
    [InlineData("", false, "An unsubscribe token is required.")]
    [InlineData("   ", false, "An unsubscribe token is required.")]
    [InlineData("non-existent-token", false, "Invalid or expired unsubscribe link.")]
    public async Task Should_ReturnError_WhenTokenIsMissingOrNotFound(
        string? token,
        bool expectedSuccess,
        string expectedSnippet
    )
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var (success, message) = await service.UnsubscribeAsync(token, null);

        success.ShouldBe(expectedSuccess);
        message.ShouldContain(expectedSnippet);
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

        var (success, message) = await service.UnsubscribeAsync("token-artist", "Bicep");

        success.ShouldBeTrue();
        message.ShouldContain("unsubscribed from alerts for Bicep");

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

        var (success, message) = await service.UnsubscribeAsync("token-all", null);

        success.ShouldBeTrue();
        message.ShouldContain("unsubscribed from all artist alerts");

        using var verifyContext = CreateContext();
        (await verifyContext.Subscriptions.FindAsync(subIds[0]))!.IsActive.ShouldBeFalse();
        (await verifyContext.Subscriptions.FindAsync(subIds[1]))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_ReturnNotSubscribedMessage_WhenArtistIsNotActive()
    {
        await SeedUser("fan@electroniclive.com", "token-inactive", ("bicep", "London", false));

        using var context = CreateContext();
        var service = CreateService(context);

        var (success, message) = await service.UnsubscribeAsync("token-inactive", "Bicep");

        success.ShouldBeTrue();
        message.ShouldContain("not currently subscribed");
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
