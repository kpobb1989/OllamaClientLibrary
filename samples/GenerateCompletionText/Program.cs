using OllamaClientLibrary;
using OllamaClientLibrary.Constants;

using var client = new OllamaClient(new LocalOllamaOptions()
{
    Model = "llama3.2:latest", // make sure this model is available in your Ollama installation
    Temperature = Temperature.CodingOrMath
});

Console.Write("Loading...");

try
{
    var response = await client.GenerateCompletionTextAsync("Why .NET is the best platform for creating applications?");

    Console.Clear();

    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();