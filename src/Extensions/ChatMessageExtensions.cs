using OllamaClientLibrary.Dto.ChatCompletion;
using OllamaClientLibrary.Models;

namespace OllamaClientLibrary.Extensions
{
    internal static class ChatMessageExtensions
    {
        public static OllamaChatMessage AsOllamaChatMessage(this ChatMessageRequest request)
        {
            return new OllamaChatMessage
            {
                Role = request.Role,
                Content = request.Content,
                Images = request.Images
            };
        }

        public static ChatMessageRequest AsChatMessageRequest(this OllamaChatMessage request)
        {
            return new ChatMessageRequest
            {
                Role = request.Role,
                Content = request.Content,
                Images = request.Images
            };
        }
    }
}
