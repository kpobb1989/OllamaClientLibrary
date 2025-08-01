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
        public async Task<string?> GetTextCompletionAsync(string? prompt, CancellationToken ct = default)
            => await GetJsonCompletionAsync<string>(prompt, ct).ConfigureAwait(false);

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var message = prompt?.AsUserChatMessage();

            if (Options.KeepConversationHistory && message != null)
            {
                ConversationHistory.Add(message);
            }

            var response = await _httpClient
                .GetCompletionAsync<T>(GetRequest(prompt), Options.Tools?.Select(s => s.AsTool()).ToArray(), ct)
                .ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                if (Options.KeepConversationHistory)
                {
                    ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));
                }

                response = await _httpClient.GetCompletionAsync<T>(GetRequest(prompt), ct: ct).ConfigureAwait(false);
            }

            return response?.Message?.Content;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            if (Options.KeepConversationHistory && !string.IsNullOrEmpty(prompt))
            {
                ConversationHistory.Add(prompt.AsUserChatMessage());
            }

            var messageChunks = new StringBuilder();

            var tools = Options.Tools?.Select(s => s.AsTool()).ToArray();
            await foreach (var chunk in _httpClient.GetChatCompletionAsync(GetRequest(prompt), tools, ct: ct))
            {
                var content = chunk?.Message?.Content;

                if (Options.Tools != null && chunk?.Message?.ToolCalls != null)
                {
                    var toolMessages = await HandleToolCallsAsync(chunk).ConfigureAwait(false);

                    if (toolMessages.Any())
                    {
                        if (Options.KeepConversationHistory)
                        {
                            ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));
                        }

                        var response = await _httpClient.GetCompletionAsync<string>(GetRequest(prompt), ct: ct)
                            .ConfigureAwait(false);
                        content = response?.Message?.Content;
                    }
                }

                messageChunks.Append(content);

                yield return new OllamaChatMessage(MessageRole.Assistant, content);
            }

            var completeMessage = new ChatMessageRequest { Role = MessageRole.Assistant, Content = messageChunks.ToString() };

            if (Options.KeepConversationHistory)
            {
                ConversationHistory.Add(completeMessage.AsOllamaChatMessage());
            }

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

        private ChatMessageRequest[] GetRequest(string? prompt)
        {
            var messages = new List<OllamaChatMessage>();

            if (Options.KeepConversationHistory)
            {
                messages = ConversationHistory;
            }
            else
            {
                messages = new List<OllamaChatMessage>()
                {
                    new OllamaChatMessage(MessageRole.System, Options.SystemPrompt),
                    new OllamaChatMessage(MessageRole.User, prompt)
                };
            }

            return messages.Select(s => s.AsChatMessageRequest()).ToArray();
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