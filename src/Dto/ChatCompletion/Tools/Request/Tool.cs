using Newtonsoft.Json;

using System.Reflection;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools.Request
{
    internal class Tool
    {
        public string? Type { get; set; }
        public Function Function { get; set; } = new Function();

        [JsonIgnore]
        public MethodInfo? MethodInfo { get; set; }

        [JsonIgnore]
        public object? Instance { get; set; }
    }
}