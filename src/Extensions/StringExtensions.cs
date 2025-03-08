using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using System.Collections.Generic;

namespace OllamaClientLibrary.Extensions
{
    public static class StringExtensions
    {
        public static OllamaChatMessage AsUserChatMessage(this string prompt, List<object>? images = null) => new OllamaChatMessage(MessageRole.User, prompt, images);
        public static OllamaChatMessage AsAssistantChatMessage(this string prompt, List<object>? images = null) => new OllamaChatMessage(MessageRole.Assistant, prompt, images);
        public static OllamaChatMessage AsSystemChatMessage(this string prompt, List<object>? images = null) => new OllamaChatMessage(MessageRole.System, prompt, images);
        public static OllamaChatMessage AsToolChatMessage(this string prompt, List<object>? images = null) => new OllamaChatMessage(MessageRole.Tool, prompt, images);
    }
}
