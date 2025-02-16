using Newtonsoft.Json;

using System;

namespace OllamaClientLibrary.Dto.Models
{
    internal class Model
    {
        [JsonProperty("model")]
        public string? Name { get; set; }

        public long? Size { get; set; }

        [JsonProperty("modified_at")]
        public DateTime? ModifiedAt { get; set; }
    }
}
