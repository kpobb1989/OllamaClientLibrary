using Newtonsoft.Json;

using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Response;

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


        /// <summary>
        /// The tool calls
        /// </summary>
        [JsonProperty("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }
}