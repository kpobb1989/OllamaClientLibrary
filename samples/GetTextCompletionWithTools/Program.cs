using Newtonsoft.Json.Linq;

using OllamaClientLibrary;
using OllamaClientLibrary.Tools;

using System.ComponentModel;

using var client = new OllamaClient();

// When Tools are used, the model must support it, otherwise there will be an exception
var tool = ToolFactory.Create<Weather>(nameof(Weather.GetTemperatureAsync));

var response = await client.GetTextCompletionAsync("What is the weather today in Paris?", tool);

Console.WriteLine($"Temperature: {response}");

Console.ReadKey();

public class Weather
{
    [Description("Get the current weather for a location")]
    public async Task<float?> GetTemperatureAsync(
        [Description("The latitude of the location, e.g. 15")] int latitude,
        [Description("The longitude of the location, e.g. 12")] int longitude)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.open-meteo.com");

        var response = await httpClient.GetAsync($"/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,wind_speed_10m&hourly=temperature_2m,relative_humidity_2m,wind_speed_10m");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var value = JObject.Parse(json)?["current"]?["temperature_2m"]?.ToString();

        if (float.TryParse(value, out var temperature))
        {
            return temperature;
        }

        return null;
    }
}