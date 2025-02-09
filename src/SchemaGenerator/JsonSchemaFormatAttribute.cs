using System;

namespace OllamaClientLibrary.SchemaGenerator
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonSchemaFormatAttribute : Attribute
    {
        public string Format { get; set; }
        public string Pattern { get; set; }

        public JsonSchemaFormatAttribute(string format, string pattern)
        {
            Format = format;
            Pattern = pattern;
        }
    }
}
