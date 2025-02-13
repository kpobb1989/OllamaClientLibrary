using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools.Response
{
    public class Function
    {
        public string? Name { get; set; }
        public Dictionary<string, object?>? Arguments { get; set; }
    }
}