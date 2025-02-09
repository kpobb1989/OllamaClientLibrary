
using System;

namespace OllamaClientLibrary
{
    public class LocalOllamaOptions : OllamaOptions
    {
        public override string Model { get; set; } = "deepseek-r1";
        public override float? Temperature { get; set; } = Constants.Temperature.GeneralConversationOrTranslation;
        public override string Host { get; set; } = GetHost();
        public override string GenerateApi => $"api/generate";
        public override string ChatAapi => $"api/chat";
        public override string TagsApi => $"api/tags";
        public override string? ApiKey { get; set; } = null;

        private static string GetHost()
        {
            var buildAgentIp = Environment.GetEnvironmentVariable("BUILD_AGENT_IP");

            return !string.IsNullOrEmpty(buildAgentIp) ? $"http://{buildAgentIp}:11434" : "http://localhost:11434";
        }
    }
}