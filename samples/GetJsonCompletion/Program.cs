using OllamaClientLibrary;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;

using System.ComponentModel;

using var client = new OllamaClient()
{
    Options = new OllamaOptions()
    {
        Temperature = Temperature.DataCleaningOrAnalysis,
        AssistantBehavior = "You are a professional .NET developer."
    }
};

Console.Write("Loading...");

Response? response = null;

try
{
    response = await client.GetJsonCompletionAsync<Response>("Return a list of all available .NET Core versions from the past five years.");

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

foreach (var item in response.Data.OrderBy(s => s.ReleaseDate))
{
    Console.WriteLine($"Version: {item.Version}, ReleaseDate: {item.ReleaseDate}, EndOfLife: {item.EndOfLife}");
}

Console.ReadKey();

class Response
{
    public List<DotNetCoreVersion>? Data { get; set; } // You always have to create a wrapper class for the response if the data is an array
}

class DotNetCoreVersion
{
    [Description("Version number in the format of Major.Minor.Patch")]
    public string? Version { get; set; }

    [Description("Release date of the version")]
    public DateTime? ReleaseDate { get; set; }

    [Description("End of life date of the version")]
    public DateTime? EndOfLife { get; set; }
}