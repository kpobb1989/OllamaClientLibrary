using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;

using System;
using System.Collections.Generic;

namespace OllamaClientLibrary.Abstractions
{
    /// <summary>
    /// Represents the options for configuring the Ollama client.
    /// </summary>
    public class OllamaOptions
    {
        /// <summary>
        /// Gets or sets the model to be used. Defaults to the value of the "OLLAMA_MODEL" environment variable or "qwen2.5:1.5b".
        /// </summary>
        public string Model { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5:1.5b";

        /// <summary>
        /// Gets or sets the host URL of the Ollama server. Defaults to the value of the "OLLAMA_SERVER_URL" environment variable or "http://localhost:11434".
        /// </summary>
        public string Host { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_SERVER_URL") ?? "http://localhost:11434";

        /// <summary>
        /// Gets or sets the API key for authentication. Defaults to the value of the "OLLAMA_API_KEY" environment variable or null.
        /// </summary>
        public string? ApiKey { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_API_KEY") ?? null;

        /// <summary>
        /// Gets or sets the temperature for the model's response. Defaults to a general conversation or translation temperature.
        /// </summary>
        public float? Temperature { get; set; } = Constants.Temperature.GeneralConversationOrTranslation;

        /// <summary>
        /// Gets or sets the API endpoint for chat interactions. Defaults to "api/chat".
        /// </summary>
        public string ChatApi { get; set; } = $"api/chat";

        /// <summary>
        /// Gets or sets the API endpoint for retrieving local models. Defaults to "api/tags".
        /// </summary>
        public string TagsApi { get; set; } = $"api/tags";

        /// <summary>
        /// Gets or sets the API endpoint for generating embeddings. Defaults to "api/embed".
        /// </summary>
        public string EmbeddingsApi { get; set; } = $"api/embed";

        /// <summary>
        /// Gets or sets the API endpoint for pulling models. Defaults to "api/pull".
        /// </summary>
        public string PullModelApi { get; set; } = $"api/pull";

        /// <summary>
        /// Gets or sets the API endpoint for deleting models. Defaults to "api/delete".
        /// </summary>
        public string DeleteModelApi { get; set; } = $"api/delete";

        /// Gets or sets a value indicating whether to automatically install the model if it is not already installed. Defaults to false.
        /// </summary>
        public bool AutoInstallModel { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of tokens for the prompt. Defaults to 4096.
        /// </summary>
        public long MaxPromptTokenSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the timeout for HTTP requests. Defaults to 60 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Get or sets the behavior of the assistant. Defaults to "You are a world class AI Assistant".
        /// </summary>
        public string? AssistantBehavior { get; set; } = "You are a world class AI Assistant";

        /// <summary>
        /// Gets or sets the list of tools available for the assistant. Tools are not applicable for all models. Make sure the model supports tools before using them. Also tools are not applicable to the JSON responses.
        /// </summary>
        public Tool[]? Tools { get; set; }

        /// <summary>
        /// Gets the chat history.
        /// </summary>
        public List<OllamaChatMessage> ChatHistory { get; set; } = new List<OllamaChatMessage>();
    }
}
