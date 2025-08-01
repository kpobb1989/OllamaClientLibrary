using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Dto;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.EmbeddingCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Dto.Models.PullModel;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace OllamaClientLibrary.HttpClients
{
    internal class OllamaHttpClient : IOllamaHttpClient
    {
        private readonly JSchemaGenerator _jsonSchemaGenerator = new JSchemaGenerator();
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;
        private readonly JsonSerializer _jsonSerializer;

        public OllamaHttpClient(OllamaOptions options, JsonSerializer jsonSerializer)
        {
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
                Format = typeof(T) != typeof(string) && tools == null ? _jsonSchemaGenerator.Generate(typeof(T)) : null,
                Messages = messages,
                Tools = tools,
                Think = _options.ThinkingEnabled,
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
                Think = _options.ThinkingEnabled,
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
                Think = _options.ThinkingEnabled
            };

            var response = await _httpClient.ExecuteAndGetJsonAsync<EmbeddingCompletionResponse>(_options.EmbeddingsApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            return response?.Embeddings ?? Array.Empty<double[]>();
        }

        public async Task<IEnumerable<Model>> ListLocalModelsAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.ExecuteAndGetJsonAsync<ModelResponse>(_options.TagsApi, HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

            return response?.Models ?? new List<Model>();
        }

        public async Task PullModelAsync(string modelName, IProgress<OllamaPullModelProgress>? progress = null, CancellationToken ct = default)
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

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
