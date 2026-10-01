using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEventSearchService
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Matches public API parameter names 'from' and 'to'"
    )]
    Task<IReadOnlyList<EventResponse>> SearchEventsAsync(
        string? query,
        string? genre = null,
        string city = "London",
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default
    );
}
