using Newtonsoft.Json;

using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Converters;
using OllamaClientLibrary.Dto.ChatCompletion.Tools.Response;

using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal class ChatMessageResponse<TContent> where TContent : class
    {
        public MessageRole Role { get; set; }

        [JsonConverter(typeof(ContentConverter))]
        public TContent? Content { get; set; }

        [JsonProperty("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }
}