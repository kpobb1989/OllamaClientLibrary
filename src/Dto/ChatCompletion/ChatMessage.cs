using OllamaClientLibrary.Constants;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    /// <summary>
    /// Represents a chat message.
    /// </summary>
    public record ChatMessage
    {
        /// <summary>
        /// The role of the message sender.
        /// </summary>
        public MessageRole Role { get; init; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string? Content { get; init; }
    };

}
