using Newtonsoft.Json;

namespace Ollama.NET.Dto.Models
{
    public record Model
    {
        [JsonProperty("model")]
        public string? Name { get; set; }
        public long? Size { get; set; }

        [JsonProperty("modified_at")]
        //[JsonConverter(typeof(ISO8601ToDateTimeConverter))]
        public DateTime? ModifiedAt { get; set; }
    }
}
