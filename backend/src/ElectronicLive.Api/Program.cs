using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();
builder.Services.AddEventClients(builder.Configuration);
builder.Services.AddEventServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapEventEndpoints();

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
