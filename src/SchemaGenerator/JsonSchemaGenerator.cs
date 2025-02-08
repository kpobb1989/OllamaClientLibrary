using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json;

namespace OllamaClientLibrary.SchemaGenerator
{
    internal static class JsonSchemaGenerator
    {
        public static JSchema Generate<T>()
        {
            JSchemaGenerator generator = new();
            JSchema schema = generator.Generate(typeof(T));

            ApplyCustomAttributes(typeof(T), schema);

            return schema;
        }

        private static void ApplyCustomAttributes(Type type, JSchema schema)
        {
            foreach (var property in type.GetProperties())
            {
                var formatAttribute = property.GetCustomAttributes(typeof(JsonSchemaFormatAttribute), true);
                if (formatAttribute.Length > 0)
                {
                    var attribute = (JsonSchemaFormatAttribute)formatAttribute[0];
                    if (!string.IsNullOrEmpty(attribute.Format))
                    {
                        schema.Properties[property.Name].Format = attribute.Format;
                    }
                    if (!string.IsNullOrEmpty(attribute.Pattern))
                    {
                        schema.Properties[property.Name].Pattern = attribute.Pattern;
                    }
                }

                // Handle nested types
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = property.PropertyType.GetGenericArguments()[0];
                    if (schema.Properties[property.Name].Items.Count > 0)
                    {
                        ApplyCustomAttributes(itemType, schema.Properties[property.Name].Items[0]);
                    }
                }
            }
        }
    }
}
