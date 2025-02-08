using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion;

namespace OllamaClientLibrary.Extensions
{
    internal static class StringExtensions
    {
        internal static ChatMessage AsUserChatMessage(this string? content) => new() { Role = MessageRole.User, Content = content };
        internal static ChatMessage AsSystemChatMessage(this string? content) => new() { Role = MessageRole.System, Content = content };
        internal static ChatMessage AsAssistantChatMessage(this string? content) => new() { Role = MessageRole.Assistant, Content = content };
        internal static ChatMessage AsToolChatMessage(this string? content) => new() { Role = MessageRole.Tool, Content = content };
    }
}