using ElectronicLive.Api.Data;
using ElectronicLive.Api.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.UnitTests.Data;

public sealed class ElectronicLiveDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ElectronicLiveDbContext> _options;

    public ElectronicLiveDbContextTests()
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

    [Fact]
    public async Task Should_PersistAndRetrieveUser()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "fan@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedUser = await context.Users.FindAsync(userId);
            savedUser.ShouldNotBeNull();
            savedUser.Email.ShouldBe("fan@electroniclive.com");
            savedUser.UnsubscribeToken.ShouldBe(user.UnsubscribeToken);
            savedUser.CreatedAt.ShouldBe(user.CreatedAt);
        }
    }

    [Fact]
    public async Task Should_ThrowException_WhenDuplicateEmailIsInserted()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var context = CreateContext();
        context.Users.AddRange(user1, user2);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task Should_ThrowException_WhenDuplicateUnsubscribeTokenIsInserted()
    {
        var sharedToken = Guid.NewGuid().ToString("N");
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@electroniclive.com",
            UnsubscribeToken = sharedToken,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@electroniclive.com",
            UnsubscribeToken = sharedToken,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var context = CreateContext();
        context.Users.AddRange(user1, user2);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task Should_PersistAndRetrieveSubscription_WithUserRelation()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "subscriber@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "bicep",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedSubscription = await context
                .Subscriptions.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == subscription.Id);

            savedSubscription.ShouldNotBeNull();
            savedSubscription.ArtistName.ShouldBe("bicep");
            savedSubscription.City.ShouldBe("London");
            savedSubscription.IsActive.ShouldBeTrue();
            savedSubscription.User.ShouldNotBeNull();
            savedSubscription.User.Email.ShouldBe("subscriber@electroniclive.com");
        }
    }

    [Fact]
    public async Task Should_ThrowException_WhenDuplicateSubscriptionForSameUserArtistAndCityIsInserted()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "multi@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "four tet",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "four tet",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var context = CreateContext();
        context.Users.Add(user);
        context.Subscriptions.AddRange(sub1, sub2);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task Should_AllowSameArtistAndCity_ForDifferentUsers()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan1@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan2@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            ArtistName = "overmono",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            ArtistName = "overmono",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.AddRange(user1, user2);
            context.Subscriptions.AddRange(sub1, sub2);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var count = await context.Subscriptions.CountAsync(s => s.ArtistName == "overmono");
            count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task Should_CascadeDeleteSubscriptions_WhenUserIsDeleted()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "delete-me@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "jon hopkins",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            context.Subscriptions.Add(sub);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedUser = await context.Users.FindAsync(user.Id);
            savedUser.ShouldNotBeNull();
            context.Users.Remove(savedUser);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var remainingSubs = await context.Subscriptions.Where(s => s.UserId == user.Id).ToListAsync();
            remainingSubs.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Should_PersistAndRetrieveNotificationLog_WithSubscriptionRelation()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "notify@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "bonobo",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var log = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub.Id,
            EventFingerprint = "2026-11-20_alexandra-palace_bonobo",
            EventTitle = "Bonobo Live at Alexandra Palace",
            SentAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            context.Subscriptions.Add(sub);
            context.NotificationLogs.Add(log);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedLog = await context
                .NotificationLogs.Include(l => l.Subscription)
                .FirstOrDefaultAsync(l => l.Id == log.Id);

            savedLog.ShouldNotBeNull();
            savedLog.EventFingerprint.ShouldBe("2026-11-20_alexandra-palace_bonobo");
            savedLog.EventTitle.ShouldBe("Bonobo Live at Alexandra Palace");
            savedLog.Subscription.ShouldNotBeNull();
            savedLog.Subscription.ArtistName.ShouldBe("bonobo");
        }
    }

    [Fact]
    public async Task Should_ThrowException_WhenDuplicateEventFingerprintForSameSubscriptionIsInserted()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplog@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "caribou",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var log1 = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub.Id,
            EventFingerprint = "2026-12-01_roundhouse_caribou",
            EventTitle = "Caribou at Roundhouse",
            SentAt = DateTimeOffset.UtcNow,
        };

        var log2 = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub.Id,
            EventFingerprint = "2026-12-01_roundhouse_caribou",
            EventTitle = "Caribou at Roundhouse",
            SentAt = DateTimeOffset.UtcNow,
        };

        await using var context = CreateContext();
        context.Users.Add(user);
        context.Subscriptions.Add(sub);
        context.NotificationLogs.AddRange(log1, log2);

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task Should_AllowSameEventFingerprint_ForDifferentSubscriptions()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan-a@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "fan-b@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            ArtistName = "floating points",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            ArtistName = "floating points",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var fingerprint = "2026-10-15_fabric_floating-points";
        var log1 = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub1.Id,
            EventFingerprint = fingerprint,
            EventTitle = "Floating Points Live",
            SentAt = DateTimeOffset.UtcNow,
        };
        var log2 = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub2.Id,
            EventFingerprint = fingerprint,
            EventTitle = "Floating Points Live",
            SentAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.AddRange(user1, user2);
            context.Subscriptions.AddRange(sub1, sub2);
            context.NotificationLogs.AddRange(log1, log2);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var logs = await context.NotificationLogs.Where(l => l.EventFingerprint == fingerprint).ToListAsync();
            logs.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task Should_CascadeDeleteNotificationLogs_WhenSubscriptionIsDeleted()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "cascade-log@electroniclive.com",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ArtistName = "aphex twin",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var log = new NotificationLog
        {
            Id = Guid.NewGuid(),
            SubscriptionId = sub.Id,
            EventFingerprint = "2026-11-05_field-day_aphex-twin",
            EventTitle = "Aphex Twin Field Day",
            SentAt = DateTimeOffset.UtcNow,
        };

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            context.Subscriptions.Add(sub);
            context.NotificationLogs.Add(log);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedSub = await context.Subscriptions.FindAsync(sub.Id);
            savedSub.ShouldNotBeNull();
            context.Subscriptions.Remove(savedSub);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var remainingLogs = await context.NotificationLogs.Where(l => l.SubscriptionId == sub.Id).ToListAsync();
            remainingLogs.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Should_NormalizeEmailToLowercaseAndTrim_WhenEmailHasWhitespaceOrUppercase()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "  User.Name@Example.COM  ",
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        user.Email.ShouldBe("user.name@example.com");

        await using (var context = CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var saved = await context.Users.FindAsync(user.Id);
            saved.ShouldNotBeNull();
            saved.Email.ShouldBe("user.name@example.com");
        }
    }

    [Fact]
    public async Task Should_NormalizeArtistNameToTrimmedLowercase_WhenArtistHasWhitespaceOrUppercase()
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ArtistName = "  Charlotte De Witte  ",
            City = "London",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        subscription.ArtistName.ShouldBe("charlotte de witte");
    }
}
