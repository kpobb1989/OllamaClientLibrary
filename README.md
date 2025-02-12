# OllamaClientLibrary
OllamaClientLibrary is a .NET Standard 2.1 library for interacting with the Ollama API. It provides functionality to generate text completions and chat completions using various models.

## Features
- Predefined configuration for the local Ollama setup, such as host, model, and temperature.
- Generate text completions. Provide a prompt and get a simple text response.
- Generate JSON completions with an automatically recognized JSON schema of the response DTO, so you no longer need to specify it in the prompt.
- Generate chat completions with streaming. The library provides access to the conversation history, allowing you to store it in the database if needed.
- Generate embeddings for a given text. The library provides functionality to convert text into numerical vectors (embeddings) that can be used for various machine learning tasks such as similarity search, clustering, and classification. This is useful for applications like semantic search, recommendation systems, and natural language understanding.
- List available local and remote models with filtering options. Now you have access to see all models installed on your local machine, as well as all models available on [Ollama's library](https://ollama.com/library)

## Prerequisites: Install Ollama locally
1. Download and install Ollama from https://ollama.com/download
2. Run `ollama run deepseek-r1` in your terminal to start Ollama and install the required models.
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
    Model = "llama3.2:latest", // Default model is "deepseek-r1". Ensure this model is available in your Ollama installation.
    Temperature = Temperature.DataCleaningOrAnalysis, // Default temperature is Temperature.GeneralConversationOrTranslation
    ApiKey = "your-api-key" // Optional. It is not required by default, so it is set to null.
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
    public string? Version { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public DateTime? EndOfLife { get; set; }
}
```


### HTTP Request / Response samples

<details>
<summary>POST Request</summary>

```json
{
    "model": "llama3.2:latest",
    "prompt": "You are a professional .NET developer. List all available .NET Core versions from the past five years.",
    "format": {
        "definitions": {
            "DotNetCoreVersion": {
                "type": [
                    "object",
                    "null"
                ],
                "properties": {
                    "Version": {
                        "type": [
                            "string",
                            "null"
                        ],
                        "pattern": "^([0-9]+).([0-9]+).([0-9]+)$",
                        "format": "string"
                    },
                    "ReleaseDate": {
                        "type": [
                            "string",
                            "null"
                        ],
                        "format": "date-time"
                    },
                    "EndOfLife": {
                        "type": [
                            "string",
                            "null"
                        ],
                        "format": "date-time"
                    }
                },
                "required": [
                    "Version",
                    "ReleaseDate",
                    "EndOfLife"
                ]
            }
        },
        "type": "object",
        "properties": {
            "Data": {
                "type": [
                    "array",
                    "null"
                ],
                "items": {
                    "$ref": "#/definitions/DotNetCoreVersion"
                }
            }
        },
        "required": [
            "Data"
        ]
    },
    "stream": false,
    "options": {
        "temperature": 1.0
    }
}
```
</details>
<details>
<summary>POST Response</summary>

```json
{
    "model": "llama3.2:latest",
    "created_at": "2025-02-09T18:38:40.4047518Z",
    "response": "{ \"Data\": [ { \"Version\": \"3.1.0\", \"ReleaseDate\": \"2019-02-07T00:00:00.000Z\" , \"EndOfLife\":\"2022-05-01T00:00:00.000Z\"}, { \"Version\": \"3.1.1\", \"ReleaseDate\": \"2019-11-06T00:00:00.000Z\" , \"EndOfLife\":\"2022-08-02T00:00:00.000Z\"}, { \"Version\": \"3.1.2\", \"ReleaseDate\": \"2019-12-10T00:00:00.000Z\" , \"EndOfLife\":\"2022-11-01T00:00:00.000Z\"}, { \"Version\": \"3.2.0\", \"ReleaseDate\": \"2020-02-13T00:00:00.000Z\" , \"EndOfLife\":\"2023-10-01T00:00:00.000Z\"}, { \"Version\": \"3.2.1\", \"ReleaseDate\": \"2020-05-07T00:00:00.000Z\" , \"EndOfLife\":\"2023-02-28T00:00:00.000Z\"}, { \"Version\": \"3.2.2\", \"ReleaseDate\": \"2020-06-18T00:00:00.000Z\" , \"EndOfLife\":\"2024-01-01T00:00:00.000Z\"}, { \"Version\": \"3.3.0\", \"ReleaseDate\": \"2021-04-26T00:00:00.000Z\" , \"EndOfLife\":\"2025-07-02T00:00:00.000Z\"}, { \"Version\": \"3.3.1\", \"ReleaseDate\": \"2021-08-16T00:00:00.000Z\" , \"EndOfLife\":\"2026-04-01T00:00:00.000Z\"}, { \"Version\": \"3.3.2\", \"ReleaseDate\": \"2021-10-25T00:00:00.000Z\" , \"EndOfLife\":\"2027-05-03T00:00:00.000Z\"} ]}",
    "done": true,
    "done_reason": "stop",
    "context": [...],
    "total_duration": 11793412400,
    "load_duration": 3542816600,
    "prompt_eval_count": 46,
    "prompt_eval_duration": 212000000,
    "eval_count": 495,
    "eval_duration": 8036000000
}
```
</details>

## More samples
- [Chat Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GetChatCompletion/Program.cs)
- [Generate JSON Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateJsonCompletion/Program.cs)
- [Generate Text Completions](https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/GenerateTextCompletion/Program.cs)
- [List Local Models](
https://github.com/kpobb1989/OllamaClientLibrary/tree/master/samples/ListLocalModels/Program.cs)
- [List Remote Models](https://github.com/kpobb1989/OllamaClientLibrary/blob/master/samples/ListRemoteModels/Program.cs)

## License
This project is licensed under the MIT License.

## Repository
For more information, visit the [GitHub repository](https://github.com/kpobb1989/OllamaClientLibrary).

## Author
Oleksandr Kushnir
