using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ElectronicLive.Api.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public IArtistVerificationService ArtistVerificationService { get; set; } =
        Substitute.For<IArtistVerificationService>();
    public IEventSearchService EventSearchService { get; set; } = Substitute.For<IEventSearchService>();
    public IEmailDispatcher EmailDispatcher { get; set; } = Substitute.For<IEmailDispatcher>();

    public CustomWebApplicationFactory()
    {
        // An open SQLite in-memory connection is retained for the factory lifetime to keep tables alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                        ["Scanner:DelayBetweenArtistsMs"] = "0",
                        ["Scanner:BaseUrl"] = "http://localhost",
                    }
                );
            }
        );

        builder.ConfigureTestServices(services =>
        {
            var dbContextDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ElectronicLiveDbContext>)
                    || d.ServiceType == typeof(ElectronicLiveDbContext)
                )
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ElectronicLiveDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.RemoveAll<IArtistVerificationService>();
            services.RemoveAll<IEventSearchService>();
            services.RemoveAll<IEmailDispatcher>();

            services.AddTransient<IArtistVerificationService>(_ => ArtistVerificationService);
            services.AddTransient<IEventSearchService>(_ => EventSearchService);
            services.AddTransient<IEmailDispatcher>(_ => EmailDispatcher);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElectronicLiveDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public void ResetMocks()
    {
        ArtistVerificationService = Substitute.For<IArtistVerificationService>();
        EventSearchService = Substitute.For<IEventSearchService>();
        EmailDispatcher = Substitute.For<IEmailDispatcher>();
    }

    public async Task ExecuteDbContextAsync(Func<ElectronicLiveDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElectronicLiveDbContext>();
        await action(context);
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
