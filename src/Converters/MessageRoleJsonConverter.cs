using System.Text.Json.Serialization;
using System.Text.Json;
using Ollama.NET.Constants;

namespace Ollama.NET.Converters
{
    public class MessageRoleJsonConverter : JsonConverter<MessageRole>
    {
        public override MessageRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var roleString = reader.GetString();
            return roleString switch
            {
                "system" => MessageRole.System,
                "user" => MessageRole.User,
                "assistant" => MessageRole.Assistant,
                "tool" => MessageRole.Tool,
                _ => throw new JsonException($"Unknown role: {roleString}")
            };
        }

        public override void Write(Utf8JsonWriter writer, MessageRole value, JsonSerializerOptions options)
        {
            var roleString = value switch
            {
                MessageRole.System => "system",
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                MessageRole.Tool => "tool",
                _ => throw new JsonException($"Unknown role: {value}")
            };
            writer.WriteStringValue(roleString);
        }
    }
}
