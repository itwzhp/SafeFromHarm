using Microsoft.Extensions.Options;
using System.Text.Json;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

internal class MoodleClient
{
    private readonly HttpClient httpClient;
    private readonly MoodleOptions options;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNamingPolicy = new MoodlePropertyNamingPolicy() };

    public MoodleClient(HttpClient httpClient, IOptions<MoodleOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async ValueTask<T> CallMoodle<T>(MoodleFunctions function, params (string Key, string Value)[] parameters)
    {
        using var body = new FormUrlEncodedContent(parameters.Select(p => KeyValuePair.Create(p.Key, p.Value)));
        var uri = new Uri(options.MoodleBaseUri, $"webservice/rest/server.php?wstoken={options.MoodleToken}&wsfunction={function}&moodlewsrestformat=json");
        
        using var response = await httpClient.PostAsync(uri, body);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        CheckForErrors(responseBody);

        return JsonSerializer.Deserialize<T>(responseBody, jsonSerializerOptions)
            ?? throw new Exception($"Unexpected null when deserializing {typeof(T)} from Moodle func {function}");
    }

    private void CheckForErrors(string responseBody)
    {
        var errorResult = JsonSerializer.Deserialize<JsonElement>(responseBody);
        if(errorResult.ValueKind == JsonValueKind.Object
            && errorResult.TryGetProperty("exception", out var exceptionProperty)
            && exceptionProperty.ValueKind == JsonValueKind.String)
        {
            var errorObject = JsonSerializer.Deserialize<ExceptionResult>(responseBody, jsonSerializerOptions);
            throw new Exception($"Received error from Moodle: {errorObject}");
        }
    }

    private class MoodlePropertyNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
            => name.ToLower();
    }
}
