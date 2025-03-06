using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion;
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
using OllamaClientLibrary.Models;
using Microsoft.Extensions.DependencyInjection;
using OllamaClientLibrary.Abstractions.HttpClients;
using OllamaClientLibrary.Abstractions.Services;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using SizeConverter = OllamaClientLibrary.Converters.SizeConverter;

namespace OllamaClientLibrary
{
    public sealed class OllamaClient : IOllamaClient
    {
        private const string RemoteModelsCacheKey = "remote-models";
        private readonly IOllamaHttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly IFileService _fileService;

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

            if (!string.IsNullOrEmpty(options.AssistantBehavior))
            {
                ConversationHistory.Insert(0, new OllamaChatMessage(MessageRole.System, options.AssistantBehavior));
            }

            // DI
            services ??= new ServiceCollection();
            services.AddOllamaClient(options);

            var serviceProvider = services.BuildServiceProvider();

            Options = serviceProvider.GetRequiredService<OllamaOptions>();
            _httpClient = serviceProvider.GetRequiredService<IOllamaHttpClient>();
            _cacheService = serviceProvider.GetRequiredService<ICacheService>();
            _fileService = serviceProvider.GetRequiredService<IFileService>();
        }

        public async Task<string?> GetTextCompletionAsync(string? prompt, OllamaFile? attachment = null, CancellationToken ct = default)
            => await GetJsonCompletionAsync<string>(prompt, attachment, ct).ConfigureAwait(false);

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt, OllamaFile? attachment = null, CancellationToken ct = default) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var message = prompt?.AsUserChatMessage();

            if (message != null)
            {
                if (attachment != null)
                {
                    if (attachment.UseOcrToExtractText)
                    {
                        var text = await _fileService.GetTextAsync(attachment.FileName, attachment.FileStream);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            message.Content += $" File: {text}";
                        }
                        else if (attachment.IsPdf())
                        {
                            var images = await _fileService.PdfToImagesAsync(attachment.FileName, attachment.FileStream);

                            foreach (var image in images)
                            {
                                message.Images.Add(image);
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"File type {attachment.FileName} is not supported.");
                        }
                    }
                    else if (attachment.IsImage())
                    {
                        // Reset to beginning of stream
                        attachment.FileStream.Position = 0;
                        // Load the image from stream
                        using var image = Image.FromStream(attachment.FileStream);
                        image.AdjustOrientation();
                        using var resizedImage = image.Resize(600, 800);
                        await using var ms = new MemoryStream();
                        resizedImage.Save(ms, ImageFormat.Jpeg);

                        message.Images.Add(ms.ToArray());
                    }
                    else
                    {
                        throw new ArgumentException($"File type {attachment.FileName} is not supported.");
                    }
                }

                ConversationHistory.Add(message);
            }

            var request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

            var response = await _httpClient.GetCompletionAsync<T>(request, Options.Tools?.Select(s => s.AsTool()).ToArray(), ct).ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();
                response = await _httpClient.GetCompletionAsync<T>(request, ct: ct).ConfigureAwait(false);
            }

            return response?.Message?.Content;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(prompt))
            {
                ConversationHistory.Add(prompt.AsUserChatMessage());
            }

            var request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

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
                        ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                        request = ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();
                        var response = await _httpClient.GetCompletionAsync<string>(request, ct: ct).ConfigureAwait(false);
                        content = response?.Message?.Content;
                    }
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
            List<OllamaModel> models;

            if (location == ModelLocation.Local)
            {
                models = (await _httpClient.ListLocalModelsAsync(ct).ConfigureAwait(false))
                    .Select(s => new OllamaModel
                    {
                        Name = s.Name, 
                        ModifiedAt = s.ModifiedAt, 
                        Size = s.Size
                    }).ToList();
            }
            else
            {
                var cache = _cacheService.Get<List<OllamaModel>>(RemoteModelsCacheKey);

                if (cache != null && cache.Any())
                {
                    models = cache;
                }
                else
                {
                    models = (await _httpClient.ListRemoteModelsAsync(ct).ConfigureAwait(false))
                        .Select(s => new OllamaModel
                        {
                            Name = s.Name, 
                            ModifiedAt = s.ModifiedAt, 
                            Size = s.Size
                        }).ToList();

                    _cacheService.Set(RemoteModelsCacheKey, models);
                }
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                models = models.Where(s => s.Name != null && Regex.IsMatch(s.Name, pattern, RegexOptions.IgnoreCase)).ToList();
            }

            if (size.HasValue)
            {
                models = size switch
                {
                    ModelSize.Tiny => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) <= 0.5).ToList(),
                    ModelSize.Small => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 0.5 && SizeConverter.BytesToGigabytes(model.Size.Value) <= 2).ToList(),
                    ModelSize.Medium => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 2 && SizeConverter.BytesToGigabytes(model.Size.Value) <= 5).ToList(),
                    ModelSize.Large => models.Where(model => model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 5).ToList(),
                    _ => models
                };
            }

            return models.OrderBy(s => s.Size ?? 0);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private async Task AutoInstallModelAsync(CancellationToken ct = default)
        {
            if (Options.AutoInstallModel)
            {
                var model = Options.Model;

                await PullModelAsync(model, ct: ct).ConfigureAwait(false);
            }
        }

        private async Task<List<ChatMessageRequest>> HandleToolCallsAsync<T>(ChatCompletionResponse<T>? response) where T : class
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
                            Content = (await ToolFactory.InvokeAsync(tool, args).ConfigureAwait(false))?.ToString()
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
