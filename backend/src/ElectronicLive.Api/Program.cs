using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();

builder.Services.Configure<TicketmasterOptions>(builder.Configuration.GetSection(TicketmasterOptions.SectionName));
builder.Services.Configure<SkiddleOptions>(builder.Configuration.GetSection(SkiddleOptions.SectionName));
builder.Services.Configure<ResidentAdvisorOptions>(
    builder.Configuration.GetSection(ResidentAdvisorOptions.SectionName)
);

builder
    .Services.AddHttpClient<ITicketmasterClient, TicketmasterClient>(
        (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TicketmasterOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        }
    )
    .AddStandardResilienceHandler();

builder
    .Services.AddHttpClient<ISkiddleClient, SkiddleClient>(
        (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SkiddleOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        }
    )
    .AddStandardResilienceHandler();

builder
    .Services.AddHttpClient<IResidentAdvisorClient, ResidentAdvisorClient>(
        (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ResidentAdvisorOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }
    )
    .AddStandardResilienceHandler();

builder.Services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<ITicketmasterClient>());
builder.Services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<ISkiddleClient>());
builder.Services.AddTransient<IEventProvider>(sp => sp.GetRequiredService<IResidentAdvisorClient>());
builder.Services.AddSingleton<IEventDeduplicator, EventDeduplicator>();
builder.Services.AddTransient<IEventAggregatorService, EventAggregatorService>();

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
