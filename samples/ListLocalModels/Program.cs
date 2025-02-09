using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.Models;

using var client = new OllamaClient();

Console.Write("Loading...");

IEnumerable<Model>? localModels = null;

try
{
    localModels = await client.ListModelsAsync(location: ModelLocation.Local);

    Console.Clear();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

if (localModels != null)
{
    foreach (var (model, index) in localModels.OrderBy(s => s.ModifiedAt).Select((model, index) => (model, index + 1)))
    {
        Console.WriteLine($"{index}) Name:{model.Name} Size:{SizeConverter.BytesToGigabytes(model.Size)}GB ModifiedAt: {model.ModifiedAt}");
    }
}

Console.ReadKey();