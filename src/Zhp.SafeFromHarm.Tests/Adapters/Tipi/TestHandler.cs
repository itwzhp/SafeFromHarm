using System.Net;

namespace Zhp.SafeFromHarm.Tests.Adapters.Tipi;

internal class TestHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public Dictionary<string, string> ResponseBody { get; } = [];

    override protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (!ResponseBody.TryGetValue(path, out var body))
            throw new InvalidOperationException($"No response set up for '{path}'. Set up: {string.Join(", ", ResponseBody.Keys)}");

        return Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
    }
}