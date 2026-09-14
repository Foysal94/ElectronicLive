using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEventDeduplicator
{
    IReadOnlyList<EventResponse> Deduplicate(IEnumerable<EventResponse> events);
}
