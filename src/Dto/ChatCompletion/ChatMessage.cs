using OllamaClientLibrary.Constants;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    /// <summary>
    /// Represents a chat message.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// The role of the message sender.
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string? Content { get; set; }
    };
}