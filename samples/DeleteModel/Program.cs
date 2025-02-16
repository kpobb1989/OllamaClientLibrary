
using OllamaClientLibrary;

using var client = new OllamaClient();

Console.Write("Enter the name of the model to uninstall: ");

var modelName = Console.ReadLine()!;

try
{

    await client.DeleteModelAsync(modelName);
}
catch (Exception ex)

{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();