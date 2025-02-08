using Ollama.NET.Converters;

using System.Text.Json.Serialization;

namespace Ollama.NET.Dto.ChatCompletion
{
    internal record ChatCompletionResponse
    {
        [JsonPropertyName("created_at")]
        [JsonConverter(typeof(ISO8601ToDateTimeConverter))]
        public DateTime? CreatedAt { get; init; }

        public ChatMessage? Message { get; init; }

        public string? Model { get; init; }
    }
}