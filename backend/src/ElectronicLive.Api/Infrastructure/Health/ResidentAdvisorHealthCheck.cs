using ElectronicLive.Api.Clients;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElectronicLive.Api.Infrastructure.Health;

public sealed class ResidentAdvisorHealthCheck(
    IResidentAdvisorClient client,
    HybridCache cache,
    ILogger<ResidentAdvisorHealthCheck> logger
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
                "health:provider:residentadvisor",
                async ct => await client.ProbeHealthAsync(ct),
                tags: HealthTags,
                cancellationToken: cancellationToken
            );

            return isHealthy
                ? HealthCheckResult.Healthy("Resident Advisor GraphQL reachable.")
                : HealthCheckResult.Degraded("Resident Advisor GraphQL unreachable.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resident Advisor health check probe failed.");
            return HealthCheckResult.Degraded($"Resident Advisor health probe failed: {ex.Message}", ex);
        }
    }
}
