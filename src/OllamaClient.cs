using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Extensions;
using OllamaClientLibrary.HttpClients;
using OllamaClientLibrary.Models;
using OllamaClientLibrary.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary
{
    public sealed class OllamaClient : IOllamaClient
    {
        private readonly IOllamaHttpClient _httpClient;

        public OllamaOptions Options { get; }

        public List<OllamaChatMessage> ConversationHistory { get; set; } = new List<OllamaChatMessage>();

        public OllamaClient() : this(new OllamaOptions())
        {
        }

        public OllamaClient(OllamaOptions? options = null) : this(options, new ServiceCollection())
        {
        }

        public OllamaClient(OllamaOptions? options = null, IServiceCollection? services = null)
        {
            options ??= new OllamaOptions();

            if (!string.IsNullOrEmpty(options.SystemPrompt))
            {
                ConversationHistory.Insert(0, new OllamaChatMessage(MessageRole.System, options.SystemPrompt));
            }

            // DI
            services ??= new ServiceCollection();
            services.AddTransient<IOllamaHttpClient, OllamaHttpClient>();
            services.AddTransient<IOllamaClient, OllamaClient>();
            services.AddSingleton(options ?? new OllamaOptions());
            services.AddSingleton(JsonSerializer.Create(new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter>()
                {
                    new StringEnumConverter(new CamelCaseNamingStrategy())
                }
            }));

            var serviceProvider = services.BuildServiceProvider();

            Options = serviceProvider.GetRequiredService<OllamaOptions>();
            _httpClient = serviceProvider.GetRequiredService<IOllamaHttpClient>();
        }

        /// <summary>
        /// Gets the current conversation messages.
        /// If KeepConversationHistory is true, returns the ConversationHistory.
        /// Otherwise, returns a new in-memory collection for this call.
        /// </summary>
        /// <returns>The list of chat messages.</returns>
        private List<OllamaChatMessage> GetMessages()
        {
            if (Options.KeepConversationHistory)
            {
                return ConversationHistory;
            }
            else
            {
                return new List<OllamaChatMessage>() { new OllamaChatMessage() { Role = MessageRole.System, Content = Options.SystemPrompt } };
            }
        }

        public async Task<OllamaChatMessage> GetCompletionAsync(string? prompt, CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var messages = GetMessages();

            var message = prompt?.AsUserChatMessage();
            if (message != null)
            {
                messages.Add(message);
            }

            var request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

            var response = await _httpClient
                .GetCompletionAsync<string>(request, Options.Tools?.Select(s => s.AsTool()).ToArray(), ct)
                .ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                messages.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

                response = await _httpClient.GetCompletionAsync<string>(request, ct: ct).ConfigureAwait(false);
            }

            if (response?.Message?.Content != null)
            {
                messages.Add(new OllamaChatMessage(MessageRole.Assistant, response.Message.Content));
            }

            return new OllamaChatMessage()
            {
                Content = response?.Message?.Content,
                Thinking = response?.Message?.Thinking,
                Role = response?.Message?.Role ?? MessageRole.Assistant
            };
        }

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var messages = GetMessages();

            var message = prompt?.AsUserChatMessage();
            if (message != null)
            {
                messages.Add(message);
            }

            var request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

            var response = await _httpClient
                .GetCompletionAsync<T>(request, Options.Tools?.Select(s => s.AsTool()).ToArray(), ct)
                .ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                messages.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

                response = await _httpClient.GetCompletionAsync<T>(request, ct: ct).ConfigureAwait(false);
            }

            if (response?.Message?.Content != null)
            {
                messages.Add(new OllamaChatMessage(MessageRole.Assistant, response.Message.Content));
            }

            return response?.Message?.Content;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var messages = GetMessages();

            if (!string.IsNullOrEmpty(prompt))
            {
                messages.Add(prompt.AsUserChatMessage());
            }

            var request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

            var messageChunks = new StringBuilder();

            var tools = Options.Tools?.Select(s => s.AsTool()).ToArray();
            await foreach (var chunk in _httpClient.GetChatCompletionAsync(request, tools, ct: ct))
            {
                var content = chunk?.Message?.Content;

                if (Options.Tools != null && chunk?.Message?.ToolCalls != null)
                {
                    var toolMessages = await HandleToolCallsAsync(chunk).ConfigureAwait(false);

                    if (toolMessages.Any())
                    {
                        messages.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                        request = messages.Select(s => s.AsChatMessageRequest()).ToArray();

                        var response = await _httpClient.GetCompletionAsync<string>(request, ct: ct)
                            .ConfigureAwait(false);
                        content = response?.Message?.Content;
                    }
                }

                messageChunks.Append(content);

                var assistantMsg = new OllamaChatMessage(MessageRole.Assistant, content);
                messages.Add(assistantMsg);

                yield return assistantMsg;
            }

            var completeMessage = new ChatMessageRequest { Role = MessageRole.Assistant, Content = messageChunks.ToString() };

            messages.Add(completeMessage.AsOllamaChatMessage());

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

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private async Task AutoInstallModelAsync(CancellationToken ct = default)
        {
            if (Options.AutoInstallModel)
            {
                var models = await _httpClient.ListLocalModelsAsync(ct).ConfigureAwait(false);

                if (models == null || !models.Any(s => string.Equals(s.Name, Options.Model, StringComparison.OrdinalIgnoreCase)))
                {
                    await _httpClient.PullModelAsync(Options.Model, null, ct).ConfigureAwait(false);
                }
            }
        }

        private async Task<List<ChatMessageRequest>> HandleToolCallsAsync<T>(ChatCompletionResponse<T>? response)
            where T : class
        {
            var toolMessages = new List<ChatMessageRequest>();

            if (Options.Tools != null && response?.Message?.ToolCalls != null)
            {
                var tasks = response.Message.ToolCalls.Select(async toolCall =>
                {
                    if (toolCall.Function is { Name: var name, Arguments: var args } &&
                        Options.Tools.FirstOrDefault(t => t.Function.Name == name) is { } tool)
                    {
                        var message = new ChatMessageRequest
                        {
                            Role = MessageRole.Tool,
                            Content = await ToolFactory.InvokeAsync(tool, args).ConfigureAwait(false)
                        };

                        return message;
                    }

                    return null;
                });

                var messages = await Task.WhenAll(tasks).ConfigureAwait(false);

                toolMessages.AddRange(messages.Where(m => m != null)!);
            }

            return toolMessages;
        }
    }
}