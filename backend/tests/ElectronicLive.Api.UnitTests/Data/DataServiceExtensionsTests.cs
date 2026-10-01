using ElectronicLive.Api.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLive.Api.UnitTests.Data;

public class DataServiceExtensionsTests
{
    [Fact]
    public void Should_RegisterElectronicLiveDbContext_WhenConnectionStringIsProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=testdb;Username=postgres;Password=postgres",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddPersistence(configuration);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ElectronicLiveDbContext>();

        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void Should_RegisterElectronicLiveDbContext_WhenSqliteConnectionStringIsProvided()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=electroniclive.db",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddPersistence(configuration);

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ElectronicLiveDbContext>();

        dbContext.ShouldNotBeNull();
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_WhenConnectionStringIsMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddPersistence(configuration));
    }

    [Theory]
    [InlineData(
        "postgresql://user:pass@ep-cool-fog.eu-west-2.aws.neon.tech/neondb?sslmode=require",
        "ep-cool-fog.eu-west-2.aws.neon.tech",
        "neondb",
        "user",
        "pass"
    )]
    [InlineData(
        "postgres://neondb_owner:secret123@ep-cool-fog.eu-west-2.aws.neon.tech:5433/customdb?sslmode=require&channel_binding=require",
        "ep-cool-fog.eu-west-2.aws.neon.tech",
        "customdb",
        "neondb_owner",
        "secret123"
    )]
    public void Should_NormalizePostgresUri_ToValidNpgsqlConnectionString(
        string uri,
        string expectedHost,
        string expectedDb,
        string expectedUser,
        string expectedPass
    )
    {
        var result = DataServiceExtensions.NormalizePostgresConnectionString(uri);

        result.ShouldContain($"Host={expectedHost}");
        result.ShouldContain($"Database={expectedDb}");
        result.ShouldContain($"Username={expectedUser}");
        result.ShouldContain($"Password={expectedPass}");
        result.ShouldContain("SSL Mode=Require");
    }

    [Fact]
    public void Should_ReturnOriginalConnectionString_WhenNotUriFormat()
    {
        const string standardString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres";
        var result = DataServiceExtensions.NormalizePostgresConnectionString(standardString);

        result.ShouldBe(standardString);
    }
}
