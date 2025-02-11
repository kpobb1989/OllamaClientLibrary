using System;

namespace OllamaClientLibrary
{
    public class OllamaOptions
    {
        public string Model { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "deepseek-r1";
        public string Host { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_SERVER_URL") ?? "http://localhost:11434";
        public string? ApiKey { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_API_KEY") ?? null;
        public float? Temperature { get; set; } = Constants.Temperature.GeneralConversationOrTranslation;
        public string GenerateApi { get; set; } = $"api/generate";
        public string ChatApi { get; set; } = $"api/chat";
        public string TagsApi { get; set; } = $"api/tags";
        public string EmbeddingsApi { get; set; } = $"api/embed";
        public bool KeepConversationHistory { get; set; } = true;
    }
}
