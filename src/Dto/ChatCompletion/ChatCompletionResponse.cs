using Newtonsoft.Json;

using System;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal class ChatCompletionResponse<TContent> where TContent : class
    {
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        public ChatMessageResponse<TContent>? Message { get; set; }

        public string? Model { get; set; }

    }
}