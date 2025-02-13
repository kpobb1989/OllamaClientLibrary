using Newtonsoft.Json;

using System.Reflection;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools
{
    public class Tool
    {
        public string Type => "function";
        public Function Function { get; set; } = new Function();

        [JsonIgnore]
        public MethodInfo? MethodInfo { get; set; } = null!;

        [JsonIgnore]
        public object? Instance { get; set; }
    }
}