using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.Dto.GenerateCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Parsers;
using OllamaClientLibrary.Dto;

using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using OllamaClientLibrary.SchemaGenerator;

namespace OllamaClientLibrary;

/// <summary>
/// Client for interacting with the Ollama API.
/// </summary>
public class OllamaClient : IDisposable
{
    private readonly string RemoteModelsCacheKey = "remote-models";
    private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
    private readonly HttpClient _httpClient;
    private readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
        Converters =
            [
                new StringEnumConverter(new CamelCaseNamingStrategy())
            ]
    });
    private readonly OllamaOptions _options;

    public OllamaClient(OllamaOptions? options = null)
    {
        _options = options ?? new LocalOllamaOptions();

        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(_options.Host)
        };

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }
    }

    /// <summary>
    /// Provides the chat history for the conversation.
    /// </summary>
    public List<ChatMessage>? ChatHistory { get; set; } = [];

    /// <summary>
    /// Generates a text response from the model. It does not track the conversation history.
    /// </summary>
    /// <param name="prompt">The prompt</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>Text</returns>
    public async Task<string?> GenerateCompletionTextAsync(string? prompt, CancellationToken ct = default)
    {
        var request = new GenerateCompletionRequest
        {
            Model = _options.Model,
            Options = new ModelOptions(_options.Temperature),
            Prompt = prompt,
            Stream = false
        };

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, request, ct);

        var response = _jsonSerializer.Deserialize<GenerateCompletionResponse>(stream);

        return response?.Response;
    }

    /// <summary>
    /// Generates a response from the model and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="prompt">The prompt</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>An object of type T</returns>
    public async Task<T?> GenerateCompletionAsync<T>(string? prompt, CancellationToken ct = default)
    {
        var schema = JsonSchemaGenerator.Generate<T>();

        var message = new GenerateCompletionRequest
        {
            Model = _options.Model,
            Options = new ModelOptions(_options.Temperature),
            Prompt = prompt,
            Format = schema,
            Stream = false
        };

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, message, ct);

        var response = _jsonSerializer.Deserialize<GenerateCompletionResponse>(stream);

        if (response?.Response == null) return default;

        var completion = _jsonSerializer.Deserialize<T?>(response.Response);

        return completion;
    }

    /// <summary>
    /// Generates a chat completion response from the model as an asynchronous stream of chat messages.
    /// </summary>
    /// <param name="text">The chat message</param>
    /// <param name="options">The options for generating the response</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>An asynchronous stream of chat messages</returns>
    public async IAsyncEnumerable<ChatMessage?> GetChatCompletionAsync(string text, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ChatHistory?.Add(text.AsUserChatMessage());

        var request = new ChatCompletionRequest
        {
            Model = _options.Model,
            Options = new ModelOptions(_options.Temperature),
            Messages = ChatHistory,
            Stream = true
        };

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.ChatAapi, HttpMethod.Post, _jsonSerializer, request, ct);

        using var reader = new StreamReader(stream);

        StringBuilder messageContentBuilder = new();
        MessageRole? messageRole = null;

        while (!reader.EndOfStream)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var line = await reader.ReadLineAsync(ct);

            if (string.IsNullOrWhiteSpace(line)) continue;

            var response = _jsonSerializer.Deserialize<ChatCompletionResponse>(line);

            if (response != null)
            {
                messageRole ??= response.Message?.Role;

                if (response.Message != null && !string.IsNullOrEmpty(response.Message.Content))
                {
                    messageContentBuilder.Append(response.Message.Content);
                }

                yield return response.Message;
            }
        }

        ChatHistory?.Add(new ChatMessage()
        {
            Role = messageRole ?? MessageRole.Assistant,
            Content = messageContentBuilder.ToString()
        });
    }

    public async Task<IEnumerable<Model>> ListModelsAsync(string? pattern = null, ModelSize? size = null, ModelLocation location = ModelLocation.Remote, CancellationToken ct = default)
    {
        IEnumerable<Model> models;

        if (location == ModelLocation.Local)
        {
            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.TagsApi, HttpMethod.Get, _jsonSerializer, ct: ct);

            var response = _jsonSerializer.Deserialize<ModelResponse>(stream);

            models = response?.Models ?? [];
        }
        else
        {
            var cache = await CacheStorage.GetAsync<IEnumerable<Model>>(RemoteModelsCacheKey, RemoteModelsCacheTime, ct);

            if (cache != null && cache.Any())
            {
                models = cache;
            }
            else
            {
                models = await RemoteModelParser.ParseAsync(_httpClient, _jsonSerializer, ct);

                await CacheStorage.SaveAsync(RemoteModelsCacheKey, models, ct);
            }
        }

        if (!string.IsNullOrEmpty(pattern))
        {
            models = models.Where(s => s.Name != null && Regex.IsMatch(s.Name, pattern, RegexOptions.IgnoreCase));
        }

        if (size.HasValue)
        {
            if (size == ModelSize.Small)
            {
                models = models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) <= 2);
            }
            else if (size == ModelSize.Medium)
            {
                models = models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 2 && SizeConverter.BytesToGigabytes(model.Size.Value) <= 5);
            }
            else if (size == ModelSize.Large)
            {
                models = models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 5);
            }
        }

        return models.OrderByDescending(s => s.Name).ThenByDescending(s => s.Size).ToList();
    }

    /// <summary>
    /// Disposes the HttpClient instance.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }
}
