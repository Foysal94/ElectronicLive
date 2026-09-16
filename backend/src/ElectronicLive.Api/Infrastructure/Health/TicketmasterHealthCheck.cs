using ElectronicLive.Api.Clients;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElectronicLive.Api.Infrastructure.Health;

public sealed class TicketmasterHealthCheck(
    ITicketmasterClient client,
    HybridCache cache,
    ILogger<TicketmasterHealthCheck> logger
) : IHealthCheck
{
    private static readonly string[] HealthTags = ["health"];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var isHealthy = await cache.GetOrCreateAsync(
                "health:provider:ticketmaster",
                async ct => await client.ProbeHealthAsync(ct),
                tags: HealthTags,
                cancellationToken: cancellationToken
            );

            return isHealthy
                ? HealthCheckResult.Healthy("Ticketmaster API reachable.")
                : HealthCheckResult.Degraded("Ticketmaster API unreachable or unconfigured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ticketmaster health check probe failed.");
            return HealthCheckResult.Degraded($"Ticketmaster health probe failed: {ex.Message}", ex);
        }
    }
}
