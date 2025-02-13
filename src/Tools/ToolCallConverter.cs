using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OllamaClientLibrary.Dto.ChatCompletion;

using System;
using System.Collections.Generic;

namespace OllamaClientLibrary.Tools
{
    internal class ToolCallConverter : JsonConverter<ToolCall>
    {
        public override ToolCall ReadJson(JsonReader reader, Type objectType, ToolCall? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var toolCall = new ToolCall
            {
                Index = obj["function"]?["index"]?.Value<int>() ?? 0,
                Name = obj["function"]?["name"]?.Value<string>(),
                Arguments = new Dictionary<string, object?>()
            };

            var arguments = obj["function"]?["arguments"] as JObject;

            if (arguments != null)
            {
                foreach (var property in arguments.Properties())
                {
                    toolCall.Arguments.Add(property.Name, property.Value.ToObject<object>());
                }
            }

            return toolCall;
        }

        public override void WriteJson(JsonWriter writer, ToolCall? value, JsonSerializer serializer)
        {
        }
    }
}