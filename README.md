# OllamaClientLibrary
OllamaClientLibrary is a .NET Standard 2.1 library for interacting with the Ollama API. It provides functionality to generate text completions and chat completions using various models.

## Features
- Predefined configuration for the local Ollama setup, such as host, model, and temperature.
- Generate text completions. Provide a prompt and get a simple text response.
- Generate JSON completions with an automatically recognized JSON schema of the response DTO, so you no longer need to specify it in the prompt.
- Generate chat completions with streaming. The library provides access to the conversation history, allowing you to store it in the database if needed.
- Use chat text completion with tools. Based on the Ollama response, you can call local methods and provide parameters dynamically.
- Generate embeddings for a given text. The library provides functionality to convert text into numerical vectors (embeddings) that can be used for various machine learning tasks such as similarity search, clustering, and classification. This is useful for applications like semantic search, recommendation systems, and natural language understanding.
- List available local and remote models with filtering options. Now you have access to see all models installed on your local machine, as well as all models available on [Ollama's library](https://ollama.com/library)

## Prerequisites: Install Ollama locally
1. Download and install Ollama from https://ollama.com/download
2. Run `ollama run qwen2.5:1.5b` in your terminal to start Ollama server and install the required models.
3. (Optional) Install specific models using `ollama pull <model-name>`, e.g., `ollama pull llama3.2:latest`. A list of available models can be found on [Ollama's library](https://ollama.com/library)
4. Verify installation with `ollama list` to check installed models on your local machine

## Installation
You can install the package via NuGet:
```
dotnet add package OllamaClientLibrary
```
## Usage
### Generate JSON Completion sample
```
// Setup OllamaClient
using var client = new OllamaClient(new OllamaOptions() // If no options are provided, OllamaOptions will be used by default.
{
    Host = "http://localhost:11434", // Default host is http://localhost:11434
    Model = "qwen2.5:1.5b", // Default model is "qwen2.5:1.5b". Ensure this model is available in your Ollama installation.
    Temperature = Temperature.DataCleaningOrAnalysis, // Default temperature is Temperature.GeneralConversationOrTranslation
    KeepChatHistory = false, // Default is true. The library will keep the chat history in memory.
    ApiKey = "your-api-key" // Optional. It is not required by default for the local setup
});

// Call Ollama API
var response = await client.GenerateJsonCompletionAsync<Response>(
    "You are a professional .NET developer. List all available .NET Core versions from the past five years."
);

// Display results
if (response?.Data != null)
{
    foreach (var item in response.Data.OrderBy(s => s.ReleaseDate))
    {
        Console.WriteLine($"Version: {item.Version}, Release Date: {item.ReleaseDate}, End of Life: {item.EndOfLife}");
    }
}

// Configure the response DTO
class Response
{
    public List<DotNetCoreVersion>? Data { get; set; } // A wrapper class is required for the response if the data is an array.
}

class DotNetCoreVersion
{
    [JsonSchemaFormat("string", @"^([0-9]+).([0-9]+).([0-9]+)$")]
    [Description("Version number in the format of Major.Minor.Patch")]
    public string? Version { get; set; }

    [Description("Release date of the version")]
    public DateTime? ReleaseDate { get; set; }

    [Description("End of life date of the version")]
    public DateTime? EndOfLife { get; set; }
}
```

## More samples
- [Chat Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetChatCompletion/Program.cs)
- [Chat Text Completions with Tools](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetChatTextCompletionWithTools/Program.cs)
- [Generate JSON Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateJsonCompletion/Program.cs)
- [Generate Text Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateTextCompletion/Program.cs)
- [Generate Embedding Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetEmbeddingCompletion/Program.cs)
- [List Local Models](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ListLocalModels/Program.cs)
- [List Remote Models](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/ListRemoteModels/Program.cs)

## License
This project is licensed under the MIT License.

## Repository
For more information, visit the [GitHub repository](https://github.com/kpobb1989/OllamaClientLibrary).

## Author
Oleksandr Kushnir
