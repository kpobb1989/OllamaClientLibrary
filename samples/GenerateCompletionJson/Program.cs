using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.SchemaGenerator;

using var client = new OllamaClient(new LocalOllamaOptions()
{
    Model = "llama3.2:latest", // make sure this model is available in your Ollama installation
    Temperature = Temperature.DataCleaningOrAnalysis,
});

Console.Write("Loading...");

Response? response = null;

try
{
    response = await client.GenerateCompletionJsonAsync<Response>("You are a professional .NET developer. List all available .NET Core versions from the past five years.");

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
    [JsonSchemaFormat("string", @"^([0-9]+).([0-9]+).([0-9]+)$")]
    public string? Version { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public DateTime? EndOfLife { get; set; }
}