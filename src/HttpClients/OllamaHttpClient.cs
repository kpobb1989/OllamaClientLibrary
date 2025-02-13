using HtmlAgilityPack;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using OllamaClientLibrary.Cache;

using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools;
using OllamaClientLibrary.Dto.EmbeddingCompletion;
using OllamaClientLibrary.Dto.GenerateCompletion;
using OllamaClientLibrary.Dto.Models;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.SchemaGenerator;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary.HttpClients
{
    internal class OllamaHttpClient : IDisposable
    {
        private readonly string RemoteModelsCacheKey = "remote-models";
        private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        private readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter(new CamelCaseNamingStrategy())
            }
        });

        public OllamaHttpClient(OllamaOptions? options)
        {
            _options = options ?? new OllamaOptions();

            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_options.Host)
            };

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            }
        }

        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();

        public async Task<string?> GenerateTextCompletionAsync(string? prompt, CancellationToken ct = default)
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

            var response = await _httpClient.ExecuteAndGetJsonAsync<GenerateCompletionResponse<string>>(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            return response?.Response;
        }

        public async Task<T?> GenerateJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class
        {
            var schema = JsonSchemaGenerator.Generate<T>();

            var request = new GenerateCompletionRequest
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

            var response = await _httpClient.ExecuteAndGetJsonAsync<GenerateCompletionResponse<T>>(_options.GenerateApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            if (response == null || response.Response == null) return default;

            return response.Response;
        }

        public async IAsyncEnumerable<ChatMessage?> GetChatCompletionAsync(string text, Tool? tool = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_options.KeepChatHistory)
            {
                ChatHistory?.Add(new ChatMessage()
                {
                    Role = MessageRole.User,
                    Content = text,
                });
            }

            var request = new ChatCompletionRequest
            {
                Model = _options.Model,
                Options = new ModelOptions()
                {
                    Temperature = _options.Temperature
                },
                Messages = ChatHistory,
                Stream = true,
            };

            if (tool != null)
            {
                request.Tools = new List<Tool>() { tool };
            }

            await using var stream = await _httpClient.ExecuteAndGetStreamAsync(_options.ChatApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            var conversation = new StringBuilder();
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
                        conversation.Append(response.Message.Content);
                    }

                    if (tool != null && response.Message?.ToolCalls?.FirstOrDefault()?.Arguments is { } arguments)
                    {
                        var result = Tools.Tools.Invoke(tool, arguments);

                        response.Message.Content = result?.ToString();
                    }

                    yield return response.Message;
                }
            }

            if (_options.KeepChatHistory && conversation.Length > 0)
            {
                ChatHistory?.Add(new ChatMessage()
                {
                    Role = messageRole ?? MessageRole.Assistant,
                    Content = conversation.ToString()
                });
            }
        }

        public async Task<string?> GetChatTextCompletionAsync(string text, Tool? tool = null, CancellationToken ct = default)
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
                    new ChatMessage()
                    {
                        Role = MessageRole.User,
                        Content = text
                    },
                },
                Stream = false,
            };

            if (tool != null)
            {
                request.Tools = new List<Tool>() { tool };
            }

            var response = await _httpClient.ExecuteAndGetJsonAsync<ChatCompletionResponse>(_options.ChatApi, HttpMethod.Post, _jsonSerializer, request, ct).ConfigureAwait(false);

            if (tool != null && response?.Message?.ToolCalls?.FirstOrDefault()?.Arguments is { } arguments)
            {
                var result = Tools.Tools.Invoke(tool, arguments);

                response.Message.Content = result?.ToString();
            }

            return response?.Message?.Content;
        }

        public async Task<double[][]> GetEmbeddingAsync(string[] input, CancellationToken ct = default)
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
                                if (float.TryParse(sizeText.Replace("GB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024); // Convert GB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("MB"))
                            {
                                if (float.TryParse(sizeText.Replace("MB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024); // Convert MB to bytes
                                }
                            }
                            else if (sizeText.EndsWith("TB"))
                            {
                                if (float.TryParse(sizeText.Replace("TB", "").Trim(), out float size))
                                {
                                    remoteModel.Size = (long)Math.Round(size * 1024 * 1024 * 1024 * 1024); // Convert TB to bytes
                                }
                            }

                            // Extract modified date
                            var dateText = parts[2].Trim();
                            if (DateTime.TryParse(dateText, out DateTime modifiedAt))
                            {
                                remoteModel.ModifiedAt = modifiedAt;
                            }
                            else
                            {
                                remoteModel.ModifiedAt = dateText.AsDateTime();
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
