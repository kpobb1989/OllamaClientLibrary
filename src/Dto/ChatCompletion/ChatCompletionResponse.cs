using Newtonsoft.Json;

namespace Ollama.NET.Dto.ChatCompletion
{
    internal record ChatCompletionResponse
    {
        [JsonProperty("created_at")]
     //   [JsonConverter(typeof(ISO8601ToDateTimeConverter))]
        public DateTime? CreatedAt { get; init; }

        public ChatMessage? Message { get; init; }

        public string? Model { get; init; }
    }
}