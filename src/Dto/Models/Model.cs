using Ollama.NET.Converters;

using System.Text.Json.Serialization;

namespace Ollama.NET.Dto.Models
{
    public record Model
    {
        [JsonPropertyName("model")]
        public string? Name { get; set; }
        public long? Size { get; set; }

        [JsonPropertyName("modified_at")]
        [JsonConverter(typeof(ISO8601ToDateTimeConverter))]
        public DateTime? ModifiedAt { get; set; }
    }
}
