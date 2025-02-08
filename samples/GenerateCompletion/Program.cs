using Ollama.NET;
using Ollama.NET.Constants;
using Ollama.NET.SchemaGenerator;

using var client = new OllamaClient(new LocalOllamaOptions()
{
    Model = "llama3.2:latest", // make sure this model is available in your Ollama installation
    Temperature = Temperature.DataCleaningOrAnalysis,
});

var completion = await client.GenerateCompletionAsync<Response>("You are a professional .net developer. Provide a list of all available .NET Core versions for the last 5 years");

if (completion == null || completion.Data == null)
{
    Console.WriteLine("No response received from the model.");

    return;
}

foreach (var item in completion.Data.OrderBy(s => s.Version))
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