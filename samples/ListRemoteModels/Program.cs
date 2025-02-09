using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.Models;

using var client = new OllamaClient();

Console.Write("Loading...");

IEnumerable<Model>? remoteModels = null;

try
{
    remoteModels = await client.ListModelsAsync(pattern: "deepseek", size: ModelSize.Small, location: ModelLocation.Remote);

    Console.Clear();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

if (remoteModels != null)
{
    foreach (var (model, index) in remoteModels.OrderBy(s => s.ModifiedAt).Select((model, index) => (model, index + 1)))
    {
        Console.WriteLine($"{index}) Name:{model.Name} Size:{SizeConverter.BytesToGigabytes(model.Size)}GB ModifiedAt: {model.ModifiedAt}");
    }
}

Console.ReadKey();