using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Models;

using var client = new OllamaClient();

Console.Write("Loading...");

IEnumerable<OllamaModel>? remoteModels = null;

try
{
    remoteModels = await client.ListModelsAsync(size: ModelSize.Small, location: ModelLocation.Remote);

    Console.Clear();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

if (remoteModels != null)
{
    foreach (var (model, index) in remoteModels.Select((model, index) => (model, index + 1)))
    {
        Console.WriteLine($"{index}) {model.Name} Size:{SizeConverter.BytesToGigabytes(model.Size):F2}GB ModifiedAt: {model.ModifiedAt}");
    }
}

Console.ReadKey();