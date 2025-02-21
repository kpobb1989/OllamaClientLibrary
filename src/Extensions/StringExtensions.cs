using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;

namespace OllamaClientLibrary.Extensions
{
    public static class StringExtensions
    {
        public static OllamaChatMessage AsUserMessage(this string prompt) => new OllamaChatMessage(MessageRole.User, prompt);
        public static OllamaChatMessage AsAssistantMessage(this string prompt) => new OllamaChatMessage(MessageRole.Assistant, prompt);
        public static OllamaChatMessage AsSystemMessage(this string prompt) => new OllamaChatMessage(MessageRole.System, prompt);
        public static OllamaChatMessage AsToolMessage(this string prompt) => new OllamaChatMessage(MessageRole.Tool, prompt);
    }
}
