using ElectronicLive.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Data;

public class ElectronicLiveDbContext : DbContext
{
    public ElectronicLiveDbContext(DbContextOptions<ElectronicLiveDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.UnsubscribeToken).IsRequired().HasMaxLength(64);

            builder.HasIndex(u => u.UnsubscribeToken).IsUnique();

            builder.Property(u => u.CreatedAt).IsRequired();

            builder
                .HasMany(u => u.Subscriptions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(builder =>
        {
            builder.ToTable("Subscriptions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.ArtistName).IsRequired().HasMaxLength(256);

            builder.Property(s => s.City).IsRequired().HasMaxLength(100).HasDefaultValue("London");

            builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(s => s.CreatedAt).IsRequired();

            builder
                .HasIndex(s => new
                {
                    s.UserId,
                    s.ArtistName,
                    s.City,
                })
                .IsUnique();

            builder
                .HasMany(s => s.NotificationLogs)
                .WithOne(n => n.Subscription)
                .HasForeignKey(n => n.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationLog>(builder =>
        {
            builder.ToTable("NotificationLogs");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.EventFingerprint).IsRequired().HasMaxLength(256);

            builder.Property(n => n.EventTitle).IsRequired().HasMaxLength(500);

            builder.Property(n => n.SentAt).IsRequired();

            builder.HasIndex(n => new { n.SubscriptionId, n.EventFingerprint }).IsUnique();
        });
    }
}
