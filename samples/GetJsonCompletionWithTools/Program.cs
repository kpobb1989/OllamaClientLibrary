using Newtonsoft.Json.Linq;

using OllamaClientLibrary;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Tools;

using System.ComponentModel;

using var client = new OllamaClient(new OllamaOptions()
{
    Temperature = Temperature.CodingOrMath,
    AssistantBehavior = "You are a professional meteorologist.",
    Tools = ToolFactory.Create<WeatherService>(nameof(WeatherService.GetTimeZoneAsync))
});

Console.Write("Loading...");

Response? response = null;

try
{
    response = await client.GetJsonCompletionAsync<Response>("Return a list of time zones for the following coordinates: 1) Latitude: 49.2331, Longitude: 28.4682, 2) Latitude: 37.7799, Longitude: -121.9780");

    Console.Clear();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

if (response == null || response.Data == null)
{
    Console.WriteLine("No response received from the model.");
    return;
}

foreach (var item in response.Data)
{
    Console.WriteLine($"TimeZone: {item.TimeZone}, Latitude: {item.Latitude}, Longitude: {item.Longitude}");
}

Console.ReadKey();

class Response
{
    public List<WeatherInfo>? Data { get; set; } // You always have to create a wrapper class for the response if the data is an array
}

class WeatherInfo
{
    [Description("A time zone based on the latitude and longitude, e.g., Europe/Paris")]
    public string? TimeZone { get; set; }

    [Description("The latitude of the location, e.g., 15")]
    public float? Latitude { get; set; }

    [Description("The longitude of the location, e.g., 12")]
    public float? Longitude { get; set; }
}

public class WeatherService : IDisposable
{
    private HttpClient httpClient = new HttpClient()
    {
        BaseAddress = new Uri("https://api.open-meteo.com")
    };

    [Description("Get a time zone based on the latitude and longitude")]
    public async Task<string?> GetTimeZoneAsync(
            [Description("The latitude of the location, e.g., 15")] float latitude,
            [Description("The longitude of the location, e.g., 12")] float longitude)
    {
        var response = await ExecuteAndGetJsonAsync($"/v1/forecast?latitude={latitude}&longitude={longitude}&timezone=auto");

        var timezone = response?["timezone"]?.ToString();

        return timezone;
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
