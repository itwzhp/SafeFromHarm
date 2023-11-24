using System.Text.RegularExpressions;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

internal class MoodleTestHttpHandler : HttpMessageHandler
{
    public string? Scenario { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var func = Regex.Match(request.RequestUri?.Query ?? throw new Exception(), @"wsfunction=(?<funcName>[^&]+)[&$]").Groups["funcName"].Value;

        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(await FindResponse(func, cancellationToken), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
        };
    }

    private async Task<string> FindResponse(string func, CancellationToken cancellationToken)
    {
        if (Scenario != null)
        {
            var scenarioPath = $"Adapters/Moodle/Responses/Scenarios/{Scenario}/{func}.json";
            if (File.Exists(scenarioPath))
                return await File.ReadAllTextAsync(scenarioPath, cancellationToken);
        }

        return await File.ReadAllTextAsync($"Adapters/Moodle/Responses/{func}.json", cancellationToken);
    }
}
