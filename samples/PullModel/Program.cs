using OllamaClientLibrary;
using OllamaClientLibrary.Models;

using var client = new OllamaClient();

Console.Write("Enter the name of the model to install: ");

var modelName = Console.ReadLine()!;

try
{

    var progress = new Progress<OllamaPullModelProgress>(progress =>
    {
        Console.Write($"\rProgress: {Math.Round(progress.Percentage)}% - Status: {progress.Status}");
    });

    await client.PullModelAsync(modelName, progress);
}
catch (OperationCanceledException)
{
}
catch (Exception ex)

{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();