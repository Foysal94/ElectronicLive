using ElectronicLive.Api.Clients;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElectronicLive.Api.Infrastructure.Health;

public sealed class SkiddleHealthCheck(ISkiddleClient client, HybridCache cache, ILogger<SkiddleHealthCheck> logger)
    : IHealthCheck
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
                "health:provider:skiddle",
                async ct => await client.ProbeHealthAsync(ct),
                tags: HealthTags,
                cancellationToken: cancellationToken
            );

            return isHealthy
                ? HealthCheckResult.Healthy("Skiddle API reachable.")
                : HealthCheckResult.Degraded("Skiddle API unreachable or unconfigured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skiddle health check probe failed.");
            return HealthCheckResult.Degraded($"Skiddle health probe failed: {ex.Message}", ex);
        }
    }
}
