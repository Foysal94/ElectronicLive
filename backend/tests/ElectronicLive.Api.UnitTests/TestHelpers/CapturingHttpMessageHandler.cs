namespace ElectronicLive.Api.UnitTests.TestHelpers;

public sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public bool WasCanceledDuringSend { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequest = request;
        WasCanceledDuringSend = cancellationToken.IsCancellationRequested;

        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return handler(request);
    }
}
