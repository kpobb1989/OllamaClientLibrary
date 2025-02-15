using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;

using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal class ChatCompletionRequest
    {
        public string? Model { get; set; }
        public IEnumerable<ChatMessageRequest>? Messages { get; set; }
        public bool Stream { get; set; }
        public ModelOptions? Options { get; set; }
        public object? Format { get; set; }
        public IEnumerable<Tool>? Tools { get; set; }
    }
}
