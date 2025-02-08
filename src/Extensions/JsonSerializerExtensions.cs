using Newtonsoft.Json;

namespace OllamaClientLibrary.Extensions
{
    internal static class JsonSerializerExtensions
    {
        public static T? Deserialize<T>(this JsonSerializer serializer, Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var response = serializer.Deserialize<T>(jsonReader);

            return response;
        }

        public static T? Deserialize<T>(this JsonSerializer serializer, string text)
        {
            using var reader = new StringReader(text);
            using var textReader = new JsonTextReader(reader);
            var response = serializer.Deserialize<T>(textReader);

            return response;
        }

        public static string Serialize<T>(this JsonSerializer serializer, T value)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            serializer.Serialize(jsonWriter, value);
            return stringWriter.ToString();
        }
    }
}
