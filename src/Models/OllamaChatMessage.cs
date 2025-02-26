using OllamaClientLibrary.Constants;

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

        public OllamaChatMessage()
        {
        }

        public OllamaChatMessage(MessageRole role, object? content)
        {
            Role = role;
            Content = content;
        }
    }
}
