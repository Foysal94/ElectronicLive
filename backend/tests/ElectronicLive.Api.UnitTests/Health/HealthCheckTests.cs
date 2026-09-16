using ElectronicLive.Api.Clients;
using ElectronicLive.Api.Infrastructure.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectronicLive.Api.UnitTests.Health;

public class HealthCheckTests
{
    private readonly ITicketmasterClient _ticketmasterClient = Substitute.For<ITicketmasterClient>();
    private readonly ISkiddleClient _skiddleClient = Substitute.For<ISkiddleClient>();
    private readonly IResidentAdvisorClient _raClient = Substitute.For<IResidentAdvisorClient>();
    private readonly HybridCache _cache;

    public HealthCheckTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task TicketmasterHealthCheck_Should_ReturnHealthy_WhenProbeSucceeds()
    {
        _ticketmasterClient.ProbeHealthAsync(Arg.Any<CancellationToken>()).Returns(true);
        var check = new TicketmasterHealthCheck(
            _ticketmasterClient,
            _cache,
            NullLogger<TicketmasterHealthCheck>.Instance
        );

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Ticketmaster API reachable");
    }

    [Fact]
    public async Task TicketmasterHealthCheck_Should_ReturnDegraded_WhenProbeFails()
    {
        _ticketmasterClient.ProbeHealthAsync(Arg.Any<CancellationToken>()).Returns(false);
        var check = new TicketmasterHealthCheck(
            _ticketmasterClient,
            _cache,
            NullLogger<TicketmasterHealthCheck>.Instance
        );

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task SkiddleHealthCheck_Should_ReturnHealthy_WhenProbeSucceeds()
    {
        _skiddleClient.ProbeHealthAsync(Arg.Any<CancellationToken>()).Returns(true);
        var check = new SkiddleHealthCheck(_skiddleClient, _cache, NullLogger<SkiddleHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ResidentAdvisorHealthCheck_Should_ReturnHealthy_WhenProbeSucceeds()
    {
        _raClient.ProbeHealthAsync(Arg.Any<CancellationToken>()).Returns(true);
        var check = new ResidentAdvisorHealthCheck(_raClient, _cache, NullLogger<ResidentAdvisorHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthResponseWriter_Should_Return200_WhenAtLeastOneProviderHealthy()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["ticketmaster"] = new(HealthStatus.Healthy, "Reachable", TimeSpan.FromMilliseconds(10), null, null),
            ["skiddle"] = new(HealthStatus.Degraded, "Unavailable", TimeSpan.FromMilliseconds(10), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(20));

        await HealthResponseWriter.WriteResponse(context, report);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task HealthResponseWriter_Should_Return503_WhenAllProvidersUnhealthyOrDegraded()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["ticketmaster"] = new(HealthStatus.Degraded, "Unavailable", TimeSpan.FromMilliseconds(10), null, null),
            ["skiddle"] = new(HealthStatus.Degraded, "Unavailable", TimeSpan.FromMilliseconds(10), null, null),
            ["residentadvisor"] = new(HealthStatus.Degraded, "Unavailable", TimeSpan.FromMilliseconds(10), null, null),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(20));

        await HealthResponseWriter.WriteResponse(context, report);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }
}
