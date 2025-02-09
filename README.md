# OllamaClientLibrary
OllamaClientLibrary is a .NET Standard 2.1 library for interacting with the Ollama API. It provides functionality to generate text completions and chat completions using various models.

## Features
- Generate text completions
- Generate json completions
- Generate chat completions
- List available local and remote models with filtering options
- Configurable options for temperature, model, and API key

## Installation
You can install the package via NuGet:
```
dotnet add package OllamaClientLibrary
```
## Usage
### Generate JSON Completion sample
- Setup OllamaClient
```
using var client = new OllamaClient(new LocalOllamaOptions()
{
    Host = "http://localhost:11434", // default host is http://localhost:11434
    Model = "llama3.2:latest", // default model is "deepseek-r1". Make sure this model is available in your Ollama installation. 
    Temperature = Temperature.DataCleaningOrAnalysis, // default is Temperature.GeneralConversationOrTranslation
    ApiKey = "your-api-key" // optional, by default it is null
});
```
- Create DTO objects
```
class Response
{
    public List<DotNetCoreVersion>? Data { get; set; }
}

class DotNetCoreVersion
{
    [JsonSchemaFormat("string", @"^([0-9]+).([0-9]+).([0-9]+)$")]
    public string? Version { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public DateTime? EndOfLife { get; set; }
}
```
- Provide a prompt and get a JSON Completion
```
var response = await client.GenerateCompletionJsonAsync<Response>("You are a professional .net developer. Provide a list of all available .NET Core versions for the last 5 years");
```
- Display the result
```
    if (response?.Data != null)
    {
        foreach (var item in response.Data.OrderBy(s => s.ReleaseDate))
        {
            Console.WriteLine($"Version: {item.Version}, ReleaseDate: {item.ReleaseDate}, EndOfLife: {item.EndOfLife}");
        }
    }
```
## More samples
- [Chat Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ChatCompletion/Program.cs)
- [Generate JSON Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateCompletionJson/Program.cs)
- [Generate Text Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateCompletionText/Program.cs)
- [List Local Models](
https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ListLocalModels/Program.cs)
- [List Remote Models](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/ListRemoteModels/Program.cs)

## License
This project is licensed under the MIT License.

## Repository
For more information, visit the [GitHub repository](https://github.com/kpobb1989/OllamaClientLibrary).

## Author
Oleksandr Kushnir
