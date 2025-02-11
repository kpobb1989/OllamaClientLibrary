using System;

namespace OllamaClientLibrary
{
    /// <summary>
    /// Represents the options for configuring the Ollama client.
    /// </summary>
    public class OllamaOptions
    {
        /// <summary>
        /// Gets or sets the model to be used. Defaults to the value of the "OLLAMA_MODEL" environment variable or "deepseek-r1".
        /// </summary>
        public string Model { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "deepseek-r1";

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
        /// Gets or sets the API endpoint for generating responses. Defaults to "api/generate".
        /// </summary>
        public string GenerateApi { get; set; } = $"api/generate";

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
        /// Gets or sets a value indicating whether to keep the conversation history. Defaults to true.
        /// </summary>
        public bool KeepConversationHistory { get; set; } = true;
    }
}
