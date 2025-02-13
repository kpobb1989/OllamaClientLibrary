using Newtonsoft.Json;

using OllamaClientLibrary.Converters;

using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    internal class GenerateCompletionResponse<T> where T: class
    {
        [JsonConverter(typeof(StringToCustomTypeConverter))]
        public T? Response { get; set; }

        public string? Model { get; set; }
    }
}
