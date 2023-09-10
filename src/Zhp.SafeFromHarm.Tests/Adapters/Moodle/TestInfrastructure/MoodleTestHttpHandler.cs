using System.Text.RegularExpressions;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

internal class MoodleTestHttpHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var func = Regex.Match(request.RequestUri?.Query ?? throw new Exception(), @"wsfunction=(?<funcName>[^&]+)[&$]").Groups["funcName"].Value;

        var returnBody = await File.ReadAllTextAsync($"Adapters/Moodle/Responses/{func}.json", cancellationToken);

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(returnBody, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
        };
    }
}
