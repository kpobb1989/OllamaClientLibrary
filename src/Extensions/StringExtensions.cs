using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

namespace OllamaClientLibrary.Extensions
{
    public static class StringExtensions
    {
        public static OllamaChatMessage AsUserChatMessage(this string prompt) => new OllamaChatMessage(MessageRole.User, prompt);
        public static OllamaChatMessage AsAssistantChatMessage(this string prompt) => new OllamaChatMessage(MessageRole.Assistant, prompt);
        public static OllamaChatMessage AsSystemChatMessage(this string prompt) => new OllamaChatMessage(MessageRole.System, prompt);
        public static OllamaChatMessage AsToolChatMessage(this string prompt) => new OllamaChatMessage(MessageRole.Tool, prompt);
    }
}
