using Ollama.NET;
using Ollama.NET.Constants;
using Ollama.NET.SchemaGenerator;

using var client = new OllamaClient(new LocalOllamaOptions()
{
    Host = "http://localhost:11434", // default Ollama server URL
    Model = "deepseek-r1", // make sure this model is available in your Ollama installation
    Temperature = Temperature.DataCleaningOrAnalysis,
});

var completion = await client.GenerateCompletionAsync<Response>("You are a professional .net developer. Provide a list of all available .net core versions for the last 5 years");

Console.ReadKey();

class Response
{
    public List<DotNetCoreVersion> Data { get; set; } = []; // You always have to create a wrapper class for the response if the data is an array
}

class DotNetCoreVersion
{
    [JsonSchemaFormat("string", @"^([0-9]+).([0-9]+).([0-9]+)$")]
    public string Version { get; set; } = null!;
    public DateTime ReleaseDate { get; set; }
    public DateTime EndOfLife { get; set; }
}