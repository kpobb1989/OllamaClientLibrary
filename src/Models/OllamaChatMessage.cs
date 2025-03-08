using OllamaClientLibrary.Constants;

using System.Collections.Generic;

namespace OllamaClientLibrary.Models
{
    public sealed class OllamaChatMessage
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

        public OllamaChatMessage()
        {
        }

        public OllamaChatMessage(MessageRole role, object? content, List<object>? images = null)
        {
            Role = role;
            Content = content;
            Images = images ?? new List<object>();
        }
    }
}
