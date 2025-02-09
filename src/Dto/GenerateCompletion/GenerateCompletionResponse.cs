using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    public record GenerateCompletionResponse<T>
    {
        [JsonConverter(typeof(StringToCustomTypeConverter))]
        public T? Response { get; init; }

        public string? Model { get; init; }
    }


    public class StringToCustomTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var responseString = reader.Value?.ToString();
            return responseString != null ? JsonConvert.DeserializeObject(responseString, objectType) : null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var responseString = JsonConvert.SerializeObject(value);
            writer.WriteValue(responseString);
        }
    }
}
