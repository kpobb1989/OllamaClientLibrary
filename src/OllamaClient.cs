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
using System.Drawing.Imaging;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;
using UglyToad.PdfPig;
using SizeConverter = OllamaClientLibrary.Converters.SizeConverter;

namespace OllamaClientLibrary
{
    public sealed class OllamaClient : IOllamaClient
    {
        private const string RemoteModelsCacheKey = "remote-models";
        private readonly IOllamaHttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly IDocumentService _documentService;
        private readonly IOcrService _ocrService;

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
            _documentService = serviceProvider.GetRequiredService<IDocumentService>();
            _ocrService = serviceProvider.GetRequiredService<IOcrService>();
        }

        public async Task<string?> GetTextCompletionFromFileAsync(string prompt, OllamaFile file,
            CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var message = prompt.AsUserChatMessage();

            if (file.IsDocument())
            {
                var extension = file.GetExtension();

                switch (extension)
                {
                    case ".doc":
                    case ".docx":
                        message.Content = _documentService.GetTextFromWord(file.FileStream, extension);
                        break;
                    case ".xls":
                    case ".xlsx":
                        message.Content = _documentService.GetTextFromExcel(file.FileStream, extension);
                        break;
                    case ".txt":
                    case ".json":
                    case ".xml":
                    case ".csv":
                        message.Content = await _documentService.GetTextAsync(file.FileStream);
                        break;
                }
            }
            else if (file.IsImage())
            {
                var text = await _ocrService.GetTextFromImageAsync(file.FileStream);

                if (!string.IsNullOrEmpty(text))
                {
                    message.Content = text;
                }
                else
                {
                    var bytes = ImageConverter.ToBytes(file.FileStream, ImageFormat.Jpeg, 600, 800);
                    message.Images.Add(bytes);
                }
            }
            else if (file.IsPdf())
            {
                var builder = new StringBuilder();
                using var document = PdfDocument.Open(file.FileStream);

                foreach (var page in document.GetPages())
                {
                    if (page.IsImageBasedPage())
                    {
                        foreach (var image in page.GetImages())
                        {
                            builder.Append(await _ocrService.GetTextFromImageAsync(image.RawBytes.ToArray()));
                        }
                    }
                    else
                    {
                        builder.Append(page.Text);
                    }
                }

                var text = builder.ToString();

                if (!string.IsNullOrEmpty(text))
                {
                    message.Content = text;
                }
                else
                {
                    foreach (var image in await PdfConverter.ToImagesAsync(file.FileStream, file.FileName))
                    {
                        message.Images.Add(image);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"File type {file.FileName} is not supported.");
            }

            ConversationHistory.Add(message);

            var response = await _httpClient.GetCompletionAsync<string>(GetRequest(), GetTools(), ct)
                .ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                response = await _httpClient.GetCompletionAsync<string>(GetRequest(), ct: ct).ConfigureAwait(false);
            }

            return response?.Message?.Content;
        }

        public async Task<string?> GetTextCompletionAsync(string? prompt, CancellationToken ct = default)
            => await GetJsonCompletionAsync<string>(prompt, ct).ConfigureAwait(false);

        public async Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            var message = prompt?.AsUserChatMessage();

            if (message != null)
            {
                ConversationHistory.Add(message);
            }

            var response = await _httpClient
                .GetCompletionAsync<T>(GetRequest(), Options.Tools?.Select(s => s.AsTool()).ToArray(), ct)
                .ConfigureAwait(false);

            var toolMessages = await HandleToolCallsAsync(response).ConfigureAwait(false);

            if (toolMessages.Any())
            {
                ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                response = await _httpClient.GetCompletionAsync<T>(GetRequest(), ct: ct).ConfigureAwait(false);
            }

            return response?.Message?.Content;
        }

        public async IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await AutoInstallModelAsync(ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(prompt))
            {
                ConversationHistory.Add(prompt.AsUserChatMessage());
            }

            var messageChunks = new StringBuilder();

            var tools = Options.Tools?.Select(s => s.AsTool()).ToArray();
            await foreach (var chunk in _httpClient.GetChatCompletionAsync(GetRequest(), tools, ct: ct))
            {
                var content = chunk?.Message?.Content;

                if (Options.Tools != null && chunk?.Message?.ToolCalls != null)
                {
                    var toolMessages = await HandleToolCallsAsync(chunk).ConfigureAwait(false);

                    if (toolMessages.Any())
                    {
                        ConversationHistory.AddRange(toolMessages.Select(m => m.AsOllamaChatMessage()));

                        var response = await _httpClient.GetCompletionAsync<string>(GetRequest(), ct: ct)
                            .ConfigureAwait(false);
                        content = response?.Message?.Content;
                    }
                }

                messageChunks.Append(content);

                yield return new OllamaChatMessage(MessageRole.Assistant, content);
            }

            var completeMessage = new ChatMessageRequest
                { Role = MessageRole.Assistant, Content = messageChunks.ToString() };

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

        public async Task PullModelAsync(string model, IProgress<OllamaPullModelProgress>? progress = null,
            CancellationToken ct = default)
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

        public async Task<IEnumerable<OllamaModel>> ListModelsAsync(string? pattern = null, ModelSize? size = null,
            ModelLocation location = ModelLocation.Remote, CancellationToken ct = default)
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
                models = models.Where(s => s.Name != null && Regex.IsMatch(s.Name, pattern, RegexOptions.IgnoreCase))
                    .ToList();
            }

            if (size.HasValue)
            {
                models = size switch
                {
                    ModelSize.Tiny => models.Where(model =>
                        model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) <= 0.5).ToList(),
                    ModelSize.Small => models.Where(model =>
                        model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 0.5 &&
                        SizeConverter.BytesToGigabytes(model.Size.Value) <= 2).ToList(),
                    ModelSize.Medium => models.Where(model =>
                        model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 2 &&
                        SizeConverter.BytesToGigabytes(model.Size.Value) <= 5).ToList(),
                    ModelSize.Large => models.Where(model =>
                        model.Size.HasValue && SizeConverter.BytesToGigabytes(model.Size.Value) > 5).ToList(),
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

        private ChatMessageRequest[] GetRequest()
            => ConversationHistory.Select(s => s.AsChatMessageRequest()).ToArray();

        private Tool[]? GetTools()
            => Options.Tools?.Select(s => s.AsTool()).ToArray();

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