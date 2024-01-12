using System.Net;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

internal class TestHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public Dictionary<string, string> ResponseBody { get; } = [];

    override protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(ResponseBody[request.RequestUri?.AbsolutePath ?? string.Empty], System.Text.Encoding.UTF8, "application/json") });
}