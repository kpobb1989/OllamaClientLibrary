using Newtonsoft.Json.Linq;

using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace OllamaClientLibrary.IntegrationTests.Tools
{
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
}
