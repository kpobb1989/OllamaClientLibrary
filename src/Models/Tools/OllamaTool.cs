using Newtonsoft.Json;

using System.Reflection;

namespace OllamaClientLibrary.Models.Tools
{
    public class OllamaTool
    {
        public string Type => "function";
        public OllamaFunction Function { get; set; } = new OllamaFunction();

        [JsonIgnore]
        public MethodInfo? MethodInfo { get; set; } = null!;

        [JsonIgnore]
        public object? Instance { get; set; }
    }
}
