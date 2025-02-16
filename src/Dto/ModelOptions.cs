using Newtonsoft.Json;

namespace OllamaClientLibrary.Dto
{
    internal class ModelOptions
    {
        public float? Temperature { get; set; }

        [JsonProperty("num_ctx")]
        public long? MaxPromptTokenSize { get; set; }
    }
}
