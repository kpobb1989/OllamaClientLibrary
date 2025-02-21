using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Cache;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Tools;
using OllamaClientLibrary.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace OllamaClientLibrary
{
    public sealed class OllamaClient : IOllamaClient
    {
        private readonly string RemoteModelsCacheKey = "remote-models";
        private readonly TimeSpan RemoteModelsCacheTime = TimeSpan.FromHours(1);
        private readonly OllamaHttpClient _httpClient;

        public OllamaOptions Options { get; set; }

        public List<OllamaChatMessage> ConversationHistory { get; set; }

        public OllamaClient()
        {
            Options = Options ?? new OllamaOptions();
            ConversationHistory = ConversationHistory ?? new List<OllamaChatMessage>();

            _httpClient = new OllamaHttpClient(Options);

            if (!string.IsNullOrEmpty(Options.AssistantBehavior))
            {
                ConversationHistory.Insert(0, new OllamaChatMessage(MessageRole.System, Options.AssistantBehavior));
            }
        }

        public async Task<string?> GetTextCompletionAsync(string? prompt = null, CancellationToken ct = default)
            => await GetJsonCompletionAsync<string>(prompt, ct).ConfigureAwait(false);

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt = null, CancellationToken ct = default) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(prompt))
            {
                ConversationHistory.Add(prompt.AsUserChatMessage());
            }

            var request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

            var response = await _httpClient.GetCompletionAsync<T>(request, Options.Tools?.Select(s => s.AsTool()).ToArray(), ct).ConfigureAwait(false);

            if (Options.Tools != null &&
                response?.Message?.ToolCalls?.FirstOrDefault()?.Function is { Name: var name, Arguments: var args } &&
                Options.Tools.FirstOrDefault(t => t.Function.Name == name) is var tool &&
                tool != null)
            {
                var toolMessage = new ChatMessageRequest
                {
                    Role = MessageRole.Tool,
                    Content = (await OllamaToolFactory.InvokeAsync(tool, args).ConfigureAwait(false))?.ToString()
                }!;

                ConversationHistory.Add(toolMessage.AsOllamaChatMessage());

                request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

                response = await _httpClient.GetCompletionAsync<T>(request, ct: ct).ConfigureAwait(false);
            }

            return response?.Message?.Content;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(prompt))
            {
                ConversationHistory.Add(prompt.AsUserChatMessage());
            }

            var request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

            var messageChunks = new StringBuilder();

            await foreach (var chunk in _httpClient.GetChatCompletionAsync(request, Options.Tools?.Select(s => s.AsTool()).ToArray(), ct:ct))
            {
                var content = chunk?.Message?.Content;

                if (Options.Tools != null &&
                    chunk?.Message?.ToolCalls?.FirstOrDefault()?.Function is { Name: var name, Arguments: var args } &&
                    Options.Tools.FirstOrDefault(t => t.Function.Name == name) is var tool && tool != null)
                {
                    var toolMessage = new ChatMessageRequest
                    {
                        Role = MessageRole.Tool,
                        Content = (await OllamaToolFactory.InvokeAsync(tool, args).ConfigureAwait(false))?.ToString()
                    }!;

                    ConversationHistory.Add(toolMessage.AsOllamaChatMessage());

                    request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

                    var response = await _httpClient.GetCompletionAsync<string>(request, ct: ct).ConfigureAwait(false);

                    content = response?.Message?.Content;
                }

                messageChunks.Append(content);

                yield return new OllamaChatMessage(MessageRole.Assistant, content);
            }

            var completeMessage = new ChatMessageRequest { Role = MessageRole.Assistant, Content = messageChunks.ToString() };

            ConversationHistory.Add(completeMessage.AsOllamaChatMessage());

            yield return new OllamaChatMessage
            {
                Content = completeMessage.Content,
                Role = completeMessage.Role
            };
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
            if (Options.AutoInstallModel)
            {
                var model = Options.Model ?? "qwen2.5:1.5b";

                await PullModelAsync(model, null, ct: ct).ConfigureAwait(false);
            }
        }

    }
}
