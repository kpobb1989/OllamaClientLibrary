
using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools.Request
{
    internal class Parameter
    {
        public string? Type { get; set; }
        public Dictionary<string, Property> Properties { get; set; } = new Dictionary<string, Property>();
        public List<string> Required { get; set; } = new List<string>();
    }
}
