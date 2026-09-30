using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Background.Email;

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
