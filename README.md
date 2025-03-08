# OllamaClientLibrary
OllamaClientLibrary is a .NET Standard 2.1 library designed to interact seamlessly with the Ollama API. It offers robust functionality for generating text and chat completions using a variety of models. This library simplifies the process of configuring and utilizing the Ollama API, making it easier for developers to integrate advanced text generation capabilities into their applications.


## Features
- Customizable configuration for Ollama (host, model, temperature, timeout, etc.)
- Automatic model installation
- JSON completions with automatic schema recognition
- Streaming chat completions with conversation history management
- Simple text completion API
- Tool-calling support for dynamic method invocation
- Text-to-embedding conversion for ML applications
- Get text completion from a file (doc, docx, xls, xlsx, pdf, txt, json, xml, csv, jpg, jpeg, png) with OCR capabilities
- Local and remote model management (list, pull, delete)

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
    AutoInstallModel = true, // Default is false
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
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;

using System.ComponentModel;

// Setup OllamaClient
using IOllamaClient client = new OllamaClient(new OllamaOptions() // If no options are provided, OllamaOptions will be used with the default settings
{
    Host = "http://localhost:11434", // Default host is http://localhost:11434
    Model = "qwen2.5:1.5b", // Default model is "qwen2.5:1.5b"
    Temperature = Temperature.DataCleaningOrAnalysis, // Default temperature is Temperature.GeneralConversationOrTranslation
    AutoInstallModel = true, // Default is false. The library will automatically install the model if it is not available on your local machine
    Timeout = TimeSpan.FromSeconds(30), // Default is 60 seconds.
    MaxPromptTokenSize = 4096, // Default is 4096 tokens. Increase this value if you want to send larger prompts
    AssistantBehavior = "You are a professional .NET developer.", // Optional. Default is "You are a world class AI Assistant"
    ApiKey = "your-api-key", // Optional. It is not required by default for the local setup
    Tools = null // Optional. You can use the ToolFactory to create tools, e.g. ToolFactory.Create<WeatherService>() where WeatherService is your class
});

// Call Ollama API
var response = await client.GetJsonCompletionAsync<Response>(
    "Return a list of all available .NET Core versions from the past five years."
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
- [Get Text Completion From a File](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetTextCompletionFromFile/Program.cs)
- [Get Chat Completion](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetChatCompletion/Program.cs)
- [Get JSON Completion](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetJsonCompletion/Program.cs)
- [Get JSON Completion with Tools](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetJsonCompletionWithTools/Program.cs)
- [Get Text Completion](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetTextCompletion/Program.cs)
- [Get Text Completion with Tools](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetTextCompletionWithTools/Program.cs)
- [Get Embedding Completion](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetEmbeddingCompletion/Program.cs) 
- [List Local Models](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ListLocalModels/Program.cs)
- [List Remote Models](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/ListRemoteModels/Program.cs)
- [Pull Model](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/PullModel/Program.cs)
- [Delete Model](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/DeleteModel/Program.cs)

## Changelog
- **v1.4.0**: Implemented `dependency injection` support, added `file-based embedding generation`, integrated document processing capabilities with `PdfPig` (PDF text extraction), `Tesseract` (`OCR` for images and image-based PDFs), and `NPOI` (extraction from DOC, DOCX, XML, XLSX formats).
- **v1.3.0**: Enhanced configuration with `MaxPromptTokenSize` and `AssistantBehavior` properties, added model deletion functionality, improved tool calling with multi-call support and async method compatibility, automated public method discovery in `ToolFactory`, and removed the `KeepChatHistory` option.
- **v1.2.0**: Improved API naming conventions, introduced `AutoInstallModel` and `Timeout` options, migrated from generate API to chat completion API, added model pull functionality, and implemented the `IOllamaClient` interface.
- **v1.1.0**: Set `qwen2.5:1.5b` as the default model, corrected `ModifiedAt` parsing for model listings, implemented tools support and conversation history, added comprehensive integration tests, and established CI pipeline.
- **v1.0.1**: Added `ApiKey` configuration support in `OllamaOptions`.
- **v1.0.0**: Initial release with core text and chat completion functionality.

## License
This project is licensed under the MIT License.

## Repository
For more information, visit the [GitHub repository](https://github.com/kpobb1989/OllamaClientLibrary).

# Dependencies
- [Tesseract](https://www.nuget.org/packages/Tesseract/) (License: Apache-2.0) - OCR capabilities
- [PdfPig](https://www.nuget.org/packages/PdfPig) (License: Apache-2.0) - Extract text from PDF files
- [NPOI](https://www.nuget.org/packages/NPOI) (License: Apache-2.0) - Extract text from Word and Excel files

## Author
Oleksandr Kushnir