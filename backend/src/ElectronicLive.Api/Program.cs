using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<TicketmasterOptions>(builder.Configuration.GetSection(TicketmasterOptions.SectionName));

builder.Services.AddHttpClient<ITicketmasterService, TicketmasterService>(
    (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<TicketmasterOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
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
