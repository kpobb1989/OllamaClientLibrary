using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    public class GenerateCompletionResponse<T>
    {
        [JsonConverter(typeof(StringToCustomTypeConverter))]
        public T Response { get; set; }

        public string Model { get; set; }
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

            if (string.IsNullOrEmpty(responseString))
            {
                return null;
            }

            if (objectType == typeof(string))
            {
                return responseString;
            }

            return responseString != null ? JsonConvert.DeserializeObject(responseString, objectType) : null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var responseString = JsonConvert.SerializeObject(value);
            writer.WriteValue(responseString);
        }
    }
}
