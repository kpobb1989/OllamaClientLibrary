using OllamaClientLibrary.Constants;

using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    /// <summary>
    /// Represents a chat message.
    /// </summary>
    internal class ChatMessageRequest
    {
        /// <summary>
        /// The role of the message sender.
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public object? Content { get; set; }

        /// <summary>
        /// The list of images associated with the message.
        /// </summary>
        public List<object> Images { get; set; } = new List<object>();

        public ChatMessageRequest()
        {
        }

        public ChatMessageRequest(MessageRole role, object? content, List<object>? images = null)
        {
            Role = role;
            Content = content;
            Images = images ?? new List<object>();
        }
    }
}