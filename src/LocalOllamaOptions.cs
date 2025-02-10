
using System;

namespace OllamaClientLibrary
{
    public class LocalOllamaOptions : OllamaOptions
    {
        public override string Model { get; set; } = "deepseek-r1";
        public override float? Temperature { get; set; } = Constants.Temperature.GeneralConversationOrTranslation;
        public override string Host { get; set; } = Environment.GetEnvironmentVariable("OLLAMA_SERVER_URL") ?? "http://localhost:11434";
        public override string GenerateApi => $"api/generate";
        public override string ChatAapi => $"api/chat";
        public override string TagsApi => $"api/tags";
        public override string? ApiKey { get; set; } = null;
    }
}