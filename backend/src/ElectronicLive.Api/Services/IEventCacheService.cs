using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEventCacheService
{
    Task<IReadOnlyList<EventResponse>> GetOrAddAsync(
        string? query,
        string? genre,
        string city,
        Func<CancellationToken, Task<IReadOnlyList<EventResponse>>> factory,
        CancellationToken cancellationToken = default
    );
}
