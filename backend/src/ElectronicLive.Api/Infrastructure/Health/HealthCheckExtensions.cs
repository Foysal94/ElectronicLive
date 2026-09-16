using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElectronicLive.Api.Infrastructure.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddEventHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<TicketmasterHealthCheck>("ticketmaster", tags: ["ready"])
            .AddCheck<SkiddleHealthCheck>("skiddle", tags: ["ready"])
            .AddCheck<ResidentAdvisorHealthCheck>("residentadvisor", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapEventHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") }
        );

        endpoints.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = HealthResponseWriter.WriteResponse,
            }
        );

        endpoints.MapHealthChecks(
            "/api/health",
            new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") }
        );

        return endpoints;
    }
}
