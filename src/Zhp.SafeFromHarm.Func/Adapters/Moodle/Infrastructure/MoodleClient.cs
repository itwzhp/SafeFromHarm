using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

internal class MoodleClient
{
    private readonly HttpClient httpClient;
    private readonly MoodleOptions options;

    public MoodleClient(HttpClient httpClient, IOptions<MoodleOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async ValueTask<T> CallMoodle<T>(MoodleFunctions function, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        using var body = new FormUrlEncodedContent(parameters);
        var uri = new Uri(options.MoodleBaseUri, $"webservice/rest/server.php?wstoken={options.MoodleToken}&wsfunction={function}&moodlewsrestformat=json");
        
        using var response = await httpClient.PostAsync(uri, body);
        response.EnsureSuccessStatusCode();

        using var responseBody = await response.Content.ReadAsStreamAsync();
        return (await JsonSerializer.DeserializeAsync<T>(responseBody))
            ?? throw new Exception($"Unexpected null when deserializing {typeof(T)} from Moodle func {function}");
    }
}
