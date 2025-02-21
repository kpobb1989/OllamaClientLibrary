using Newtonsoft.Json.Linq;

using OllamaClientLibrary;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Tools;

using System.ComponentModel;

using var client = new OllamaClient(new OllamaOptions()
{
    // When Tools are used, the model must support it, otherwise there will be an exception
    Tools = ToolFactory.CreateList<Weather>(nameof(Weather.GetTemperatureAsync), nameof(Weather.GetTimeZoneAsync))
});

var temperature = await client.GetTextCompletionAsync("What is the weather today in Paris?");

Console.WriteLine($"Temperature: {temperature}");

var timezone = await client.GetTextCompletionAsync("What's the time zone by latitude=48.8, longitude=2.3?");

Console.WriteLine($"Time zone: {timezone}");

Console.ReadKey();

public class Weather : IDisposable
{
    private HttpClient httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://api.open-meteo.com")
    };

    [Description("Get a time zone based on the latitude and longitude")]
    public async Task<string?> GetTimeZoneAsync(
            [Description("The latitude of the location, e.g. 15")] float latitude,
            [Description("The longitude of the location, e.g. 12")] float longitude)
    {
        var response = await ExecuteAndGetJsonAsync($"/v1/forecast?latitude={latitude}&longitude={longitude}&timezone=auto");

        var timezone = response?["timezone"]?.ToString();

        return timezone;
    }

    [Description("Get the current weather for a location")]
    public async Task<float?> GetTemperatureAsync(
    [Description("The latitude of the location, e.g. 15")] float latitude,
    [Description("The longitude of the location, e.g. 12")] float longitude)
    {
        var response = await ExecuteAndGetJsonAsync($"/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m");

        var value = response?["current"]?["temperature_2m"]?.ToString();

        if (float.TryParse(value, out var temperature))
        {
            return temperature;
        }

        return null;
    }

    private async Task<JObject> ExecuteAndGetJsonAsync(string url, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        return JObject.Parse(json);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}