namespace Ollama.NET
{
    public class DeepSeekOptions(string apiKey) : OllamaOptions
    {
        public override string? Model { get; set; } = "deepseek-chat";
        public override float? Temperature { get; set; } = Constants.Temperature.GeneralConversationOrTranslation;
        public override string Host { get; set; } = "https://api.deepseek.com";
        public override string GenerateApi => $"api/generate";
        public override string ChatAapi => $"chat/completions";
        public override string TagsApi => $"/api/tags";
        public override string? ApiKey => apiKey;
    }
}
