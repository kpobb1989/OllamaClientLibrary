using OllamaClientLibrary;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;

using System.ComponentModel;

// Setup OllamaClient
using IOllamaClient client = new OllamaClient(new OllamaOptions() // If no options are provided, OllamaOptions will be used with the default settings
{
    Host = "http://localhost:11434", // Default host is http://localhost:11434
    Model = "qwen2.5:1.5b", // Default model is "qwen2.5:1.5b"
    Temperature = Temperature.DataCleaningOrAnalysis, // Default temperature is Temperature.GeneralConversationOrTranslation
    AutoInstallModel = true, // Default is false. The library will automatically install the model if it is not available on your local machine
    Timeout = TimeSpan.FromSeconds(30), // Default is 60 seconds.
    MaxPromptTokenSize = 4096, // Default is 4096 tokens. Increase this value if you want to send larger prompts
    AssistantBehavior = "You are a professional .NET developer.", // Optional. Default is "You are a world class AI Assistant"
    ApiKey = "your-api-key", // Optional. It is not required by default for the local setup
    Tools = null // Optional. You can use the ToolFactory to create tools, e.g. ToolFactory.Create<WeatherService>() where WeatherService is your class
});

// Call Ollama API
var response = await client.GetJsonCompletionAsync<Response>(
    "Return a list of all available .NET Core versions from the past five years."
);

// Display results
if (response?.Data != null)
{
    foreach (var item in response.Data.OrderBy(s => s.ReleaseDate))
    {
        Console.WriteLine($"Version: {item.Version}, Release Date: {item.ReleaseDate}, End of Life: {item.EndOfLife}");
    }
}

// Configure the response DTO
class Response
{
    public List<DotNetCore>? Data { get; set; } // A wrapper class is required for the response if the data is an array.
}

class DotNetCore
{
    [Description("Version number in the format of Major.Minor.Patch")]
    public string? Version { get; set; }

    [Description("Release date of the version")]
    public DateTime? ReleaseDate { get; set; }

    [Description("End of life date of the version")]
    public DateTime? EndOfLife { get; set; }
}