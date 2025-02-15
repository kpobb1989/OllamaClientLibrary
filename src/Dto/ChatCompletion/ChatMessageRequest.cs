using OllamaClientLibrary.Constants;

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
    }
}