namespace Ollama.NET.SchemaGenerator
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonSchemaFormatAttribute : Attribute
    {
        public string Format { get; }
        public string? Pattern { get; }

        public JsonSchemaFormatAttribute(string format, string? pattern = null)
        {
            Format = format;
            Pattern = pattern;
        }
    }
}
