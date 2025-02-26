using HtmlAgilityPack;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Abstractions.Services;
using OllamaClientLibrary.Dto;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.EmbeddingCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Dto.Models.PullModel;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.Models;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace OllamaClientLibrary.HttpClients
{
    internal class OllamaHttpClient : IOllamaHttpClient
    {
        private readonly JSchemaGenerator JsonSchemaGenerator = new JSchemaGenerator();
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;
        private readonly IOllamaWebParserService _ollamaWebParserService;
        private readonly JsonSerializer _jsonSerializer;

        public OllamaHttpClient(IOllamaWebParserService ollamaWebParserService, OllamaOptions options, JsonSerializer jsonSerializer)
        {
            _ollamaWebParserService = ollamaWebParserService;
            _options = options;
            _jsonSerializer = jsonSerializer;

            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_options.Host),
                Timeout = _options.Timeout
            };

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            }
        }

        public async Task<ChatCompletionResponse<T>?> GetCompletionAsync<T>(ChatMessageRequest[] messages, Tool[]? tools = null, CancellationToken ct = default) where T : class
        {
            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature,
                    MaxPromptTokenSize = _options.MaxPromptTokenSize
                },
                Format = typeof(T) != typeof(string) && tools == null ? JsonSchemaGenerator.Generate(typeof(T)) : null,
                Messages = messages,
                Tools = tools,
                Stream = false
            };

            return await _httpClient.ExecuteAndGetJsonAsync<ChatCompletionResponse<T>>(_options.ChatApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<ChatCompletionResponse<string>> GetChatCompletionAsync(ChatMessageRequest[] messages, Tool[]? tools = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature,
                    MaxPromptTokenSize = _options.MaxPromptTokenSize
                },
                Messages = messages,
                Stream = tools == null,
                Tools = tools
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.ChatApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line)) continue;

                var response = _jsonSerializer.Deserialize<ChatCompletionResponse<string>>(line);

                if (response != null)
                {
                    yield return response;
                }
            }
        }

        public async Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default)
        {
            var request = new EmbeddingCompletionRequest
            {
                Model = _options.Model,
                Input = input,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature,
                    MaxPromptTokenSize = _options.MaxPromptTokenSize
                },
            };

            var response = await _httpClient.ExecuteAndGetJsonAsync<EmbeddingCompletionResponse>(_options.EmbeddingsApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            return response?.Embeddings ?? Array.Empty<double[]>();
        }

        public async Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.ExecuteAndGetJsonAsync<ModelResponse>(_options.TagsApi, HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

            return response?.Models ?? new List<Model>();
        }

        public async Task<IEnumerable<Model>> ListRemoteModelsAsync(CancellationToken ct = default)
        {
            using var stream = await _httpClient.ExecuteAndGetStreamAsync("https://ollama.com/library?sort=newest", HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(stream);

            var hrefs = htmlDoc.DocumentNode
                      .SelectNodes("//a[starts-with(@href, '/library/')]")
                      .Select(node => node.GetAttributeValue("href", string.Empty))
                      .ToList();

            var remoteModels = new ConcurrentBag<Model>();

            var semaphore = new SemaphoreSlim(20);

            var tasks = hrefs.Select(async href =>
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);

                try
                {
                    var models = await GetRemoteModelsAsync(href, ct).ConfigureAwait(false);

                    foreach (var model in models)
                    {
                        remoteModels.Add(model);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return remoteModels;
        }

        public async Task PullModelAsync(string modelName, IProgress<OllamaPullModelProgress>? progress, CancellationToken ct)
        {
            var request = new PullModelRequest()
            {
                Model = modelName,
                Stream = true
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.PullModelApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            double lastReportedPercentage = -1;

            while (!reader.EndOfStream)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line)) continue;

                var response = JsonConvert.DeserializeObject<PullModelResponse>(line);

                if (response != null)
                {
                    if (!string.IsNullOrEmpty(response.Error))
                    {
                        throw new InvalidOperationException($"Error pulling model: {response.Error}");
                    }

                    if (response.Percentage >= 0 && response.Percentage <= 100 && response.Percentage > lastReportedPercentage)
                    {
                        lastReportedPercentage = response.Percentage;
                        progress?.Report(new OllamaPullModelProgress
                        {
                            Status = response.Status,
                            Percentage = response.Percentage
                        });
                    }
                }

            }
        }

        public async Task DeleteModelAsync(string model, CancellationToken ct = default)
        {
            var request = new PullModelRequest()
            {
                Model = model
            };

            await _httpClient.ExecuteAsync(_options.DeleteModelApi, HttpMethod.Delete, _jsonSerializer, request, returnStream: false, ct).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private async Task<IEnumerable<Model>> GetRemoteModelsAsync(string href, CancellationToken ct)
        {
            using var stream = await _httpClient.ExecuteAndGetStreamAsync($"https://ollama.com/{href}/tags", HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

            var remoteModels = await _ollamaWebParserService.GetRemoteModelsAsync(stream, ct).ConfigureAwait(false);

            return remoteModels;
        }
    }
}
