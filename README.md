# OllamaClientLibrary
OllamaClientLibrary is a .NET Standard 2.1 library for interacting with the Ollama API. It provides functionality to generate text completions and chat completions using various models.

## Features
- Predefined configuration for the local Ollama setup, such as host, model, and temperature, timeout, etc.
- Auto-install models if they are not available on your local machine.
- Get JSON completions with an automatically recognized JSON schema of the response DTO, so you no longer need to specify it in the prompt.
- Get chat completions with streaming. The library provides access to the conversation history, allowing you to store it in the database if needed.
- Get text completions. Provide a prompt and get a simple text response.
- Get text completion with tools. Based on the Ollama response, the library can dynamically call local methods and provide the necessary parameters.
- Get embeddings completions for a given text. The library provides functionality to convert text into numerical vectors (embeddings) that can be used for various machine learning tasks such as similarity search, clustering, and classification. This is useful for applications like semantic search, recommendation systems, and natural language understanding.
- List available local and remote models with filtering options. Now you have access to see all models installed on your local machine, as well as all models available on [Ollama's library](https://ollama.com/library)

## Prerequisites: 
### Setting Up Ollama Server
Download and install Ollama from https://ollama.com/download

### Installing a Model
1. Execute `ollama run qwen2.5:1.5b` in your terminal to start the Ollama server and install the necessary models. You can find a list of available models on [Ollama's library](https://ollama.com/library).
2. Confirm the installation by running `ollama list` to see the models installed on your local machine.

### Installing a Model (Alternative)
```
using var client = new OllamaClient(new OllamaOptions()
{
    AutoInstallModel = true, // Default is false. The library will automatically install the model if it is not available on your local machine
});
```

## Installation
You can install the package via NuGet:
```
Install-Package OllamaClientLibrary
```
## Usage
### Generate JSON Completion sample
```
using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using System.ComponentModel;

// Setup OllamaClient
using var client = new OllamaClient(new OllamaOptions() // If no options are provided, OllamaOptions will be used with the default settings
{
    Host = "http://localhost:11434", // Default host is http://localhost:11434
    Model = "qwen2.5:1.5b", // Default model is "qwen2.5:1.5b"
    Temperature = Temperature.DataCleaningOrAnalysis, // Optional. Default temperature is Temperature.GeneralConversationOrTranslation
    KeepChatHistory = false, // Default is true. The library will keep the chat history in memory.
    AutoInstallModel = true, // Default is false. The library will automatically install the model if it is not available on your local machine
    Timeout = TimeSpan.FromSeconds(30), // Default is 60 seconds.
    ApiKey = "your-api-key", // Optional. It is not required by default for the local setup
});

// Call Ollama API
var response = await client.GetJsonCompletionAsync<Response>(
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
    public List<DotNetCore>? Data { get; set; } // A wrapper class is required for the response if the data is an array.
}

class DotNetCore
{
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
- [Get Embedding Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetEmbeddingCompletion/Program.cs)
- [Get JSON Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetJsonCompletion/Program.cs)
- [Get Text Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetTextCompletion/Program.cs)
- [Get Text Completions with Tools](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetTextCompletionWithTools/Program.cs)
- [List Local Models](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ListLocalModels/Program.cs)
- [List Remote Models](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/ListRemoteModels/Program.cs)

## Changelog
- **v1.2.0**: Initial release with basic functionality for text and chat completions.
- **v1.1.0**: Changed the default model to `qwen2.5:1.5b`, fixed parsing ModifiedAt for the Models list endpoint, added support for Tools, added Chat History, integration tests and configured CI.
- **v1.0.1**: Allowed setting the `ApiKey` in `OllamaOptions`.
- **v1.0.0**: Initial release with basic functionality for text and chat completions.

## License
This project is licensed under the MIT License.

## Repository
For more information, visit the [GitHub repository](https://github.com/kpobb1989/OllamaClientLibrary).

## Author
Oleksandr Kushnir