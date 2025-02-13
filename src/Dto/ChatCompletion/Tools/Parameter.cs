using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools
{
    public class Parameter
    {
        public string Type => "object";
        public Dictionary<string, Property> Properties { get; set; } = new Dictionary<string, Property>();
        public List<string> Required { get; set; } = new List<string>();
    }
}
