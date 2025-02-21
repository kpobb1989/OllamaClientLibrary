using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary
{
    public sealed class OllamaClient : IOllamaClient
    {
        private readonly string RemoteModelsCacheKey = "remote-models";
        private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
        private readonly List<ChatMessageRequest> _chatHistory = new List<ChatMessageRequest>();
        private readonly OllamaHttpClient _httpClient;
        private readonly OllamaOptions _options;

        public IEnumerable<OllamaChatMessage> ChatHistory => _chatHistory.Select(s => new OllamaChatMessage(s.Role, s.Content));

        public OllamaClient(OllamaOptions? options = null)
        {
            _options = options ?? new OllamaOptions();

            _httpClient = new OllamaHttpClient(_options);
        }

        public async Task<string?> GetTextCompletionAsync(string? prompt, Tool? tool = null, CancellationToken ct = default)
        {
            await foreach (var message in GetCompletionAsync<string>(prompt, null, tool, ct))
            {
                return message?.Content?.ToString();
            }
            return null;
        }

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class
        {
            await foreach (var message in GetCompletionAsync<T>(prompt, null, null, ct))
            {
                return message?.Content as T;
            }
            return null;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string prompt, Tool? tool = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var message in GetCompletionAsync<string>(prompt, null, tool, ct))
            {
                yield return message;
            }
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(IList<OllamaChatMessage> messages, Tool? tool = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var message in GetCompletionAsync<string>(null, messages, tool, ct))
            {
                yield return message;
            }
        }

        public async Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            return await _httpClient.GetEmbeddingCompletionAsync(input, ct).ConfigureAwait(false);
        }

        public async Task PullModelAsync(string model, IProgress<OllamaPullModelProgress>? progress = null, CancellationToken ct = default)
        {
            var models = await _httpClient.ListLocalModelsAsync(ct).ConfigureAwait(false);

            if (models == null || !models.Any(s => string.Equals(s.Name, model, StringComparison.OrdinalIgnoreCase)))
            {
                await _httpClient.PullModelAsync(model, progress, ct).ConfigureAwait(false);
            }
            else
            {
                progress?.Report(new OllamaPullModelProgress()
                {
                    Status = $"The model {model} is already installed",
                    Percentage = 100
                });
            }
        }

        public async Task DeleteModelAsync(string model, CancellationToken ct = default)
        {
            var models = await _httpClient.ListLocalModelsAsync(ct).ConfigureAwait(false);

            if (models.Any(s => string.Equals(s.Name, model, StringComparison.OrdinalIgnoreCase)))
            {
                await _httpClient.DeleteModelAsync(model, ct).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<OllamaModel>> ListModelsAsync(string? pattern = null, ModelSize? size = null, ModelLocation location = ModelLocation.Remote, CancellationToken ct = default)
        {
            IEnumerable<OllamaModel> models;

            if (location == ModelLocation.Local)
            {
                models = (await _httpClient.ListLocalModelsAsync(ct).ConfigureAwait(false)).Select(s => new OllamaModel() { Name = s.Name, ModifiedAt = s.ModifiedAt, Size = s.Size });
            }
            else
            {
                var cache = CacheStorage.Get<IEnumerable<OllamaModel>>(RemoteModelsCacheKey, RemoteModelsCacheTime);

                if (cache != null && cache.Any())
                {
                    models = cache;
                }
                else
                {
                    models = (await _httpClient.ListRemoteModelsAsync(ct).ConfigureAwait(false)).Select(s => new OllamaModel() { Name = s.Name, ModifiedAt = s.ModifiedAt, Size = s.Size });

                    CacheStorage.Save(RemoteModelsCacheKey, models);
                }
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                models = models.Where(s => s.Name != null && Regex.IsMatch(s.Name, pattern, RegexOptions.IgnoreCase));
            }

            if (size.HasValue)
            {
                models = size switch
                {
                    ModelSize.Tiny => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) <= 0.5),
                    ModelSize.Small => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 0.5 && SizeConverter.BytesToGigabytes(model.Size.Value) <= 2),
                    ModelSize.Medium => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 2 && SizeConverter.BytesToGigabytes(model.Size.Value) <= 5),
                    ModelSize.Large => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 5),
                    _ => models
                };
            }

            return models.OrderBy(s => s.Size ?? 0).ToList();
        }

        public void Dispose()
        {
            _httpClient.Dispose();

            GC.SuppressFinalize(this);
        }

        private async Task AutoInstallModelAsync(CancellationToken ct = default)
        {
            if (_options.AutoInstallModel)
            {
                var model = _options.Model ?? "qwen2.5:1.5b";

                await PullModelAsync(model, null, ct: ct).ConfigureAwait(false);
            }
        }

        private async IAsyncEnumerable<OllamaChatMessage?> GetCompletionAsync<T>(string? prompt, IList<OllamaChatMessage>? messages, Tool? tool, [EnumeratorCancellation] CancellationToken ct) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            messages ??= new List<OllamaChatMessage>();

            if (!string.IsNullOrEmpty(_options.AssistantBehavior) && messages.FirstOrDefault()?.Role != MessageRole.System)
            {
                messages.Insert(0, new OllamaChatMessage(MessageRole.System, _options.AssistantBehavior));
            }

            if (!string.IsNullOrEmpty(prompt))
            {
                messages.Add(new OllamaChatMessage(MessageRole.User, prompt));
            }

            _chatHistory.AddRange(messages.Select(s => new ChatMessageRequest { Content = s.Content, Role = s.Role }));

            var response = await _httpClient.GetCompletionAsync<T>(_chatHistory, tool, ct).ConfigureAwait(false);

            if (tool != null && response?.Message?.ToolCalls?.FirstOrDefault()?.Function?.Arguments is { } arguments)
            {
                var toolMessage = new ChatMessageRequest
                {
                    Role = MessageRole.Tool,
                    Content = (await ToolFactory.InvokeAsync(tool, arguments).ConfigureAwait(false))?.ToString()
                };

                messages.Add(new OllamaChatMessage(toolMessage.Role, toolMessage.Content));
                _chatHistory.Add(toolMessage);

                response = await _httpClient.GetCompletionAsync<T>(_chatHistory, ct: ct).ConfigureAwait(false);
            }

            var finalChatMessage = new ChatMessageRequest { Role = MessageRole.Assistant, Content = response?.Message?.Content };
            _chatHistory.Add(finalChatMessage);
            messages.Add(new OllamaChatMessage(finalChatMessage.Role, finalChatMessage.Content));

            yield return new OllamaChatMessage
            {
                Content = finalChatMessage.Content,
                Role = finalChatMessage.Role
            };
        }

    }
}
