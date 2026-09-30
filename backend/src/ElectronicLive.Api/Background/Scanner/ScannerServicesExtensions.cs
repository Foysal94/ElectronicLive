using ElectronicLive.Api.Configuration;

namespace ElectronicLive.Api.Background.Scanner;

public static class ScannerServicesExtensions
{
    public static IServiceCollection AddWatchlistScanner(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScannerOptions>(configuration.GetSection(ScannerOptions.SectionName));
        services.AddScoped<IWatchlistScannerService, WatchlistScannerService>();
        return services;
    }
}
