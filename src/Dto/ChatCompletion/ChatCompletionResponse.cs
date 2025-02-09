using Newtonsoft.Json;

using System;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal class ChatCompletionResponse
    {
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        public ChatMessage? Message { get; set; }

        public string? Model { get; set; }
    }
}