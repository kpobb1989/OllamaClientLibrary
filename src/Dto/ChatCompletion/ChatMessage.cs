using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Tools;

using System.Collections.Generic;

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

        [JsonProperty("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }

    [JsonConverter(typeof(ToolCallConverter))]
    public class ToolCall
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public Dictionary<string, object?>? Arguments { get; set; }
    }
}