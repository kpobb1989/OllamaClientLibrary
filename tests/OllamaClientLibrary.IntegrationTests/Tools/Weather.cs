using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace OllamaClientLibrary.IntegrationTests.Tools
{
    public class Weather
    {
        [Description("Get the current weather for a location")]
        public int GetTemperature(
        [Description("The location to get the weather for, e.g. San Francisco, CA")] string location,
        [Description("The format to return the weather in, e.g. 'celsius' or 'fahrenheit'")] Format format)
        {
            return 23;
        }
        public enum Format
        {
            Celsius,
            Fahrenheit
        }
    }
}
