﻿using System.Collections.Generic;

namespace OllamaClientLibrary.Models.Tools
{
    public class OllamaParameter
    {
        public string Type => "object";
        public Dictionary<string, OllamaProperty> Properties { get; set; } = new Dictionary<string, OllamaProperty>();
        public List<string> Required { get; set; } = new List<string>();
    }
}
