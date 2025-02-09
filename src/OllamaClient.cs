using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.GenerateCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.Parsers;
using OllamaClientLibrary.SchemaGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary
{
    /// <summary>
    /// Represents a client for interacting with the Ollama API.
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
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            }
        });
        private readonly OllamaOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaClient"/> class.
        /// </summary>
        /// <param name="options">The options for configuring the client.</param>
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
        /// Gets or sets the chat history.
        /// </summary>
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Generates completion text asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated completion text.</returns>
        public async Task<string?> GenerateCompletionTextAsync(string? prompt, CancellationToken ct = default)
        {
            var request = new GenerateCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Prompt = prompt,
                Stream = false
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            var response = _jsonSerializer.Deserialize<GenerateCompletionResponse<string>>(stream);

            return response?.Response;
        }

        /// <summary>
        /// Generates completion asynchronously and deserializes the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated completion deserialized to the specified type.</returns>
        public async Task<T?> GenerateCompletionJsonAsync<T>(string? prompt, CancellationToken ct = default) where T: class
        {
            var schema = JsonSchemaGenerator.Generate<T>();

            var message = new GenerateCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Prompt = prompt,
                Format = schema,
                Stream = false
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, message, ct).ConfigureAwait(false);

            var response = _jsonSerializer.Deserialize<GenerateCompletionResponse<T>>(stream);

            if (response == null || response.Response == null) return default;

            return response.Response;
        }

        /// <summary>
        /// Gets chat completion asynchronously.
        /// </summary>
        /// <param name="text">The text to get chat completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of chat messages.</returns>
        public async IAsyncEnumerable<ChatMessage?> GetChatCompletionAsync(string text, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ChatHistory?.Add(text.AsUserChatMessage());

            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Messages = ChatHistory,
                Stream = true
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.ChatAapi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            var messageContentBuilder = new StringBuilder();
            MessageRole? messageRole = null;

            while (!reader.EndOfStream)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var line = await reader.ReadLineAsync().ConfigureAwait(false);

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

        /// <summary>
        /// Lists models asynchronously.
        /// </summary>
        /// <param name="pattern">The pattern to filter models by name.</param>
        /// <param name="size">The size to filter models by.</param>
        /// <param name="location">The location to filter models by.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A list of models.</returns>
        public async Task<IEnumerable<Model>> ListModelsAsync(string? pattern = null, ModelSize? size = null, ModelLocation location = ModelLocation.Remote, CancellationToken ct = default)
        {
            IEnumerable<Model> models;

            if (location == ModelLocation.Local)
            {
                await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.TagsApi, HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

                var response = _jsonSerializer.Deserialize<ModelResponse>(stream);

                models = response?.Models ?? new List<Model>();
            }
            else
            {
                var cache = await CacheStorage.GetAsync<IEnumerable<Model>>(RemoteModelsCacheKey, RemoteModelsCacheTime).ConfigureAwait(false);

                if (cache != null && cache.Any())
                {
                    models = cache;
                }
                else
                {
                    models = await RemoteModelParser.ParseAsync(_httpClient, _jsonSerializer, ct).ConfigureAwait(false);

                    CacheStorage.Save(RemoteModelsCacheKey, models);
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
        /// Disposes the resources used by the <see cref="OllamaClient"/> class.
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}