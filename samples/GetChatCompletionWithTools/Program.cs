using OllamaClientLibrary;
using OllamaClientLibrary.Tools;

using System.ComponentModel;

using var client = new OllamaClient(new OllamaOptions()
{
    Model = "llama3.2:latest"
});

var tool = Tools.Create<Weather>(nameof(Weather.GetTemperature));

var response = await client.GetChatTextCompletionAsync("What is the weather today in Paris?", tool);

Console.WriteLine($"Response: {response}");

Console.ReadKey();

public class Weather
{
    [Description("Get the current weather for a location")]
    public int GetTemperature(
    [Description("The location to get the weather for, e.g. San Francisco, CA")] string location,
    [Description("The format to return the weather in, e.g. 'celsius' or 'fahrenheit'")] Format format)
    {
        return 0;
    }
    public enum Format
    {
        Celsius,
        Fahrenheit
    }
}