using System.Net;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

internal class TestHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = string.Empty;

    override protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json") });
}