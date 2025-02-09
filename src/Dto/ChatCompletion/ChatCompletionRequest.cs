using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal class ChatCompletionRequest
    {
        public string? Model { get; set; }
        public string? Prompt { get; set; }
        public IEnumerable<ChatMessage>? Messages { get; set; }
        public bool Stream { get; set; }
        public ModelOptions? Options { get; set; }
    }
}
