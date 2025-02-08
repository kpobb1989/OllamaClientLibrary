using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Schema;

using Ollama.NET.Cache;
using Ollama.NET.Constants;
using Ollama.NET.Converters;
using Ollama.NET.Dto;
using Ollama.NET.Dto.ChatCompletion;
using Ollama.NET.Dto.GenerateCompletion;
using Ollama.NET.Dto.Models;
using Ollama.NET.Extensions;
using Ollama.NET.Parsers;

using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ollama.NET;

/// <summary>
/// Client for interacting with the Ollama API.
/// </summary>
public class OllamaClient : IDisposable
{
    private readonly string RemoteModelsCacheKey = "remote-models";
    private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new MessageRoleJsonConverter(), new ISO8601ToDateTimeConverter() }
    };
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

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, request, _serializerOptions, ct);

        var response = await JsonSerializer.DeserializeAsync<GenerateCompletionResponse>(stream, _serializerOptions, cancellationToken: ct);

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
        var schema = SchemaGenerator.JsonSchemaGenerator.Generate<T>();

        var message = new GenerateCompletionRequest
        {
            Model = _options.Model,
            Options = new ModelOptions(_options.Temperature),
            Prompt = prompt, //. Schema: {JsonSerializer.Serialize(schema)}
            Format = schema,
            Stream = false
        };

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, message, _serializerOptions, ct);

        var response = await JsonSerializer.DeserializeAsync<GenerateCompletionResponse>(stream, _serializerOptions, cancellationToken: ct);

        if (response?.Response == null) return default;

        var completion = JsonSerializer.Deserialize<T?>(response.Response);

        return completion;
    }

    //public class Schema
    //{
    //    public string? Type { get; set; }

    //    public Dictionary<string, Property> Properties { get; set; } = [];

    //    public List<string> Required { get; set; } = [];

    //}

    //public class Property
    //{
    //    public string? Type { get; set; }
    //}

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

        await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.ChatAapi, HttpMethod.Post, request, _serializerOptions, ct);

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

            var response = JsonSerializer.Deserialize<ChatCompletionResponse>(line, _serializerOptions);

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

    public async Task<IEnumerable<Model>> ListModelsAsync(string? model = null, ModelSize? size = ModelSize.Small, ModelLocation location = ModelLocation.Remote, CancellationToken ct = default)
    {
        IEnumerable<Model> models;

        if (location == ModelLocation.Local)
        {
            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.TagsApi, HttpMethod.Get, ct: ct);

            var response = await JsonSerializer.DeserializeAsync<ModelResponse>(stream, _serializerOptions, ct);

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
                models = await RemoteModelParser.ParseAsync(_httpClient, ct);

                await CacheStorage.SaveAsync(RemoteModelsCacheKey, models, ct);
            }
        }

        if (!string.IsNullOrEmpty(model))
        {
            models = models.Where(s => s.Name != null && s.Name.Contains(model, StringComparison.OrdinalIgnoreCase));
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
