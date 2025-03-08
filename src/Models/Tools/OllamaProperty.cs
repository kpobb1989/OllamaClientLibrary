using System.Collections.Generic;

namespace OllamaClientLibrary.Models.Tools
{
    public class OllamaProperty
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public List<string>? Enum { get; set; }
    }
}
