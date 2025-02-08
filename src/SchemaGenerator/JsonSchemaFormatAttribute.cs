namespace Ollama.NET.SchemaGenerator
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonSchemaFormatAttribute(string format, string? pattern = null) : Attribute
    {
        public string Format { get; } = format;
        public string? Pattern { get; } = pattern;
    }
}
