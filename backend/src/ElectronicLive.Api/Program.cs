using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Services;

const string CorsPolicyName = "FrontendCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddOpenApi();
builder.Services.AddEventClients(builder.Configuration);
builder.Services.AddEventServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicyName);

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapEventEndpoints();

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
