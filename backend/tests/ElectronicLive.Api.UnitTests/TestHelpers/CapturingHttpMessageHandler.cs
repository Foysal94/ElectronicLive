namespace ElectronicLive.Api.UnitTests.TestHelpers;

public sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public bool WasCanceledDuringSend { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequest = request;
        WasCanceledDuringSend = cancellationToken.IsCancellationRequested;
        return Task.FromResult(handler(request));
    }
}
