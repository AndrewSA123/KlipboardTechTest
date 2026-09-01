namespace FaultTriage.Tests.FaultAnalysers;

/// <summary>
/// A test double for HttpMessageHandler that returns a caller-defined response
/// (or throws) instead of making a real network call.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }
}