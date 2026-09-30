using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Background.Scanning;
using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Data;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Services;
using Microsoft.EntityFrameworkCore;

const string CorsPolicyName = "FrontendCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    allowedOrigins = ["http://localhost:5173", "http://localhost:3000"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        CorsPolicyName,
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
            }
        }
    );
});

builder.Services.AddOpenApi();

var appInsightsConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddEventClients(builder.Configuration);
builder.Services.AddEventServices();
builder.Services.AddEventCaching(builder.Configuration);
builder.Services.AddEmailDispatching(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddWatchlistScanner(builder.Configuration);

var app = builder.Build();

// Temporary deployment guardrail: Allows initial deployment of the persistence layer to Azure
// without crashing on unprovisioned database connection strings. Once Neon credentials and Terraform
// wiring are complete in #51, this catch guardrail will be removed in favor of fail-fast startup.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ElectronicLiveDbContext>();
        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(
            ex,
            "Database initialization or migration skipped/failed on startup. Continuing startup in degraded state."
        );
    }
}

if (IsJobInvocation(args, "scan-watchlist"))
{
    return await ExecuteScanWatchlistJobAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicyName);

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapEventEndpoints();
app.MapSubscriptionEndpoints();

await app.RunAsync();
return 0;

public partial class Program
{
    protected Program() { }

    internal static bool IsJobInvocation(string[] args, string expectedJob)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, $"--job={expectedJob}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (
                string.Equals(arg, "--job", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
                && string.Equals(args[i + 1], expectedJob, StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    internal static async Task<int> ExecuteScanWatchlistJobAsync(
        IServiceProvider services,
        CancellationToken ct = default
    )
    {
        using var scope = services.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<IWatchlistScannerService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting scheduled watchlist scan job...");
            var result = await scanner.ExecuteScanAsync(ct);
            logger.LogInformation(
                "Watchlist scan completed. Artists scanned: {Artists}, Subscriptions processed: {Subscriptions}, Digests sent: {Digests}, Errors: {Errors}",
                result.ArtistsScanned,
                result.SubscriptionsProcessed,
                result.DigestsSent,
                result.ErrorsCount
            );

            return result.ErrorsCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Watchlist scan job encountered an unhandled fatal error.");
            return 1;
        }
    }
}
