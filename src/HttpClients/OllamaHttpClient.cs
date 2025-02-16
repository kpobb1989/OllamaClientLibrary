using HtmlAgilityPack;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.Dto.EmbeddingCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Dto.PullModel;
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
    internal class OllamaHttpClient : IDisposable
    {
        private readonly JSchemaGenerator JsonSchemaGenerator = new JSchemaGenerator();
        private readonly string RemoteModelsCacheKey = "remote-models";
        private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        private readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            }
        });

        public OllamaHttpClient(OllamaOptions options)
        {
            _options = options;

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

        public async Task<ChatCompletionResponse<T>?> GetCompletionAsync<T>(string? prompt, Tool? tool = null, CancellationToken ct = default) where T : class
        {
            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Messages = new[]
                {
                    new ChatMessageRequest()
                    {
                        Role = MessageRole.User,
                        Content = prompt
                    },
                },
                Format = typeof(T) != typeof(string) ? JsonSchemaGenerator.Generate(typeof(T)) : null,
                Tools = tool != null ? new List<Tool>() { tool } : null,
                Stream = tool != null,
            };

            return await _httpClient.ExecuteAndGetJsonAsync<ChatCompletionResponse<T>>(_options.ChatApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false); ;
        }

        public async IAsyncEnumerable<ChatCompletionResponse<string>> GetChatCompletionAsync(IEnumerable<ChatMessageRequest> messages, Tool? tool = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Messages = messages,
                Stream = true,
                Tools = tool != null ? new List<Tool>() { tool } : null
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
                    Temperature = _options.Temperature
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
            var cache = CacheStorage.Get<IEnumerable<Model>>(RemoteModelsCacheKey, RemoteModelsCacheTime);

            if (cache != null && cache.Any())
            {
                return cache;
            }
            else
            {
                var models = await GetRemoteModelsAsync(ct).ConfigureAwait(false);

                CacheStorage.Save(RemoteModelsCacheKey, models);

                return models;
            }
        }

        public async Task PullModelAsync(string modelName, IProgress<OllamaPullModelProgress>? progress, CancellationToken ct)
        {
            var request = new PullModelRequest()
            {
                Model = modelName,
                Stream = true
            };

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.PullApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

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

        private async Task<IEnumerable<Model>> GetRemoteModelsAsync(CancellationToken ct)
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

        private async Task<IEnumerable<Model>> GetRemoteModelsAsync(string href, CancellationToken ct)
        {
            using var stream = await _httpClient.ExecuteAndGetStreamAsync($"https://ollama.com/{href}/tags", HttpMethod.Get, _jsonSerializer, ct: ct).ConfigureAwait(false);

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(stream);

            var modelNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='flex px-4 py-3']");

            var remoteModels = new List<Model>();

            var semaphore = new SemaphoreSlim(5);

            var tasks = modelNodes.Select(async modelNode =>
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);

                try
                {

                    var remoteModel = new Model
                    {
                        Name = modelNode.SelectSingleNode(".//a[@class='group']").GetAttributeValue("href", null)?.Split("/").Last()
                    };

                    // Extract size and modified date
                    var infoNode = modelNode.SelectSingleNode(".//div[@class='flex items-baseline space-x-1 text-[13px] text-neutral-500']/span");

                    if (infoNode != null)
                    {
                        var infoText = infoNode.InnerText.Trim();
                        var parts = infoText.Split('•');

                        if (parts.Length >= 2)
                        {
                            // Extract size
                            var sizeText = parts[1].Trim();
                            if (sizeText.EndsWith("GB"))
                            {
                                if (float.TryParse(sizeText.Replace("GB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024); // Convert GB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("MB"))
                            {
                                if (float.TryParse(sizeText.Replace("MB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024); // Convert MB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("TB"))
                            {
                                if (float.TryParse(sizeText.Replace("TB", string.Empty).Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024 * 1024); // Convert TB to bytes
                                }
                            }

                            // Extract modified date
                            var dateText = parts[2].Trim();
                            if (DateTime.TryParse(dateText, out DateTime modifiedAt))
                            {
                                remoteModel.ModifiedAt = modifiedAt.Date;
                            }
                            else
                            {
                                remoteModel.ModifiedAt = dateText.AsDateTime()?.Date;
                            }
                        }
                    }

                    remoteModels.Add(remoteModel);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return remoteModels;
        }
    }
}
