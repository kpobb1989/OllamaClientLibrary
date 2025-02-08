using Newtonsoft.Json;

namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal record ChatCompletionResponse
    {
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; init; }

        public ChatMessage? Message { get; init; }

        public string? Model { get; init; }
    }
}