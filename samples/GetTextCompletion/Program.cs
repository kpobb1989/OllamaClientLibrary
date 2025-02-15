using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using var client = new OllamaClient(new OllamaOptions()
{
    Temperature = Temperature.CodingOrMath
});

Console.Write("Loading...");

try
{
    var response = await client.GetTextCompletionAsync("Why .NET is the best platform for creating applications?");

    Console.Clear();

    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();