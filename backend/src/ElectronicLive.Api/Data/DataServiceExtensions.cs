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
                options.UseNpgsql(NormalizePostgresConnectionString(connectionString));
            }
        });

        return services;
    }

    internal static string NormalizePostgresConnectionString(string connectionString)
    {
        if (
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
        )
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port > 0 ? uri.Port : 5432;

            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = Npgsql.SslMode.Require,
            };

            return builder.ConnectionString;
        }

        return connectionString;
    }

    private static bool IsSqliteConnectionString(string connectionString)
    {
        return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
            || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
    }
}
