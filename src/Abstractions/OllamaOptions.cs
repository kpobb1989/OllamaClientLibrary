using System;
using System.Net.Http;

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
        /// Gets or sets the API endpoint for retrieving tags. Defaults to "api/tags".
        /// </summary>
        public string TagsApi { get; set; } = $"api/tags";

        /// <summary>
        /// Gets or sets the API endpoint for generating embeddings. Defaults to "api/embed".
        /// </summary>
        public string EmbeddingsApi { get; set; } = $"api/embed";

        /// <summary>
        /// Gets or sets the API endpoint for pulling models. Defaults to "api/pull".
        /// </summary>
        public string PullApi { get; set; } = $"api/pull";

        /// <summary>
        /// Gets or sets a value indicating whether to keep the chat history. Defaults to true.
        /// </summary>
        public bool KeepChatHistory { get; set; } = true;


        /// Gets or sets a value indicating whether to automatically install the model if it is not already installed. Defaults to false.
        /// </summary>
        public bool AutoInstallModel { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout for HTTP requests. Defaults to 60 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    }
}
