using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

public interface IEmailDispatcher
{
    Task SendDigestAsync(
        string toEmail,
        string artistName,
        IReadOnlyList<EventResponse> newEvents,
        string unsubscribeUrl,
        CancellationToken ct = default
    );
}
