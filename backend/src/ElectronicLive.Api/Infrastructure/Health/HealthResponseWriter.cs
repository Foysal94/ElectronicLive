using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElectronicLive.Api.Infrastructure.Health;

public static class HealthResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var allProvidersFailed =
            report.Entries.Count > 0 && report.Entries.Values.All(e => e.Status != HealthStatus.Healthy);

        if (allProvidersFailed)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    description = kvp.Value.Description,
                    duration = kvp.Value.Duration.ToString(),
                    error = kvp.Value.Exception?.Message,
                }
            ),
        };

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
