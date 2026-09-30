using ElectronicLive.Api.Background.Email;
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

public partial class Program
{
    protected Program() { }
}
