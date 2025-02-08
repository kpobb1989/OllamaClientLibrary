using Ollama.NET;
using Ollama.NET.Constants;
using Ollama.NET.Converters;

using var client = new OllamaClient();

Console.Write("Loading...");

var remoteModels = await client.ListModelsAsync(pattern: "deepseek", size: ModelSize.Medium, location: ModelLocation.Remote);

Console.Clear();

foreach (var (model, index) in remoteModels.OrderBy(s => s.ModifiedAt).Select((model, index) => (model, index + 1)))
{
    Console.WriteLine($"{index}) Name:{model.Name} Size:{SizeConverter.BytesToGigabytes(model.Size)}GB ModifiedAt: {model.ModifiedAt}");
}

Console.ReadKey();