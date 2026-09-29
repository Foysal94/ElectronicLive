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
}
