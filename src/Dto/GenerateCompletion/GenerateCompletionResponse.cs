using Newtonsoft.Json;

using OllamaClientLibrary.Converters;

namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    internal class GenerateCompletionResponse<T>
    {
        [JsonConverter(typeof(StringToCustomTypeConverter))]
        public T Response { get; set; }

        public string? Model { get; set; }
    }
}
