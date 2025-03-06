using Newtonsoft.Json;

using System;
using System.IO;

namespace OllamaClientLibrary.Converters
{
    internal class ContentConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (!(reader.Value is string json)) return existingValue;
            
            if (objectType == typeof(string))
            {
                return json;
            }

            using var stringReader = new StringReader(json);
            using var jsonReader = new JsonTextReader(stringReader);

            return serializer.Deserialize(jsonReader, objectType);

        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
