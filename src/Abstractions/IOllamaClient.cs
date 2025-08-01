
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaClientLibrary.Abstractions
{
    /// <summary>
    /// Defines the interface for interacting with the Ollama API.
    /// </summary>
    public interface IOllamaClient : IDisposable
    {
        /// <summary>
        /// Gets the options for the Ollama client.
        /// </summary>
        public OllamaOptions Options { get; }

        /// <summary>
        /// Gets or sets the conversation history.
        /// </summary>
        public List<OllamaChatMessage> ConversationHistory { get; set; }
        
        /// <summary>
        /// Gets chat completion asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt to get chat completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of chat messages.</returns>
        IAsyncEnumerable<OllamaChatMessage?> GetChatCompletionAsync(string? prompt, CancellationToken ct = default);

        /// <summary>
        /// Gets embeddings for the specified input asynchronously.
        /// </summary>
        /// <param name="input">The input text to generate embeddings for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a jagged array of doubles representing the embeddings.</returns>
        Task<double[][]> GetEmbeddingCompletionAsync(string[] input, CancellationToken ct = default);

        /// <summary>
        /// Gets JSON completion asynchronously and deserialize the response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated completion deserialized to the specified type.</returns>
        Task<T?> GetJsonCompletionAsync<T>(string? prompt, CancellationToken ct = default) where T : class;

        /// <summary>
        /// Gets the completion asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt to generate completion for.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The generated completion.</returns>
        Task<OllamaChatMessage> GetCompletionAsync(string? prompt, CancellationToken ct = default);
    }
}