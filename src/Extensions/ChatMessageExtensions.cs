using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Dto.ChatCompletion;

namespace OllamaClientLibrary.Extensions
{
    internal static class ChatMessageExtensions
    {
        public static OllamaChatMessage AsOllamaChatMessage(this ChatMessageRequest request)
        {
            return new OllamaChatMessage
            {
                Role = request.Role,
                Content = request.Content
            };
        }

        public static ChatMessageRequest AsChatMessageRequest(this OllamaChatMessage message)
        {
            return new ChatMessageRequest
            {
                Role = message.Role,
                Content = message.Content
            };
        }
    }
}
