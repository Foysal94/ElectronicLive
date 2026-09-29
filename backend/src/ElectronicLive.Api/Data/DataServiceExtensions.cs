using Microsoft.EntityFrameworkCore;

namespace ElectronicLive.Api.Data;

public static class DataServiceExtensions
{
    public const string DefaultConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionStringName}' is not configured."
            );
        }

        services.AddDbContext<ElectronicLiveDbContext>(options =>
        {
            if (IsSqliteConnectionString(connectionString))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }

    private static bool IsSqliteConnectionString(string connectionString)
    {
        return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }
}
