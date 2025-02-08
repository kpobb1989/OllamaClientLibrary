using Newtonsoft.Json;

namespace OllamaClientLibrary.Dto.Models
{
    public record Model
    {
        [JsonProperty("model")]
        public string? Name { get; set; }
        public long? Size { get; set; }

        [JsonProperty("modified_at")]
        public DateTime? ModifiedAt { get; set; }
    }
}
