﻿using System.Collections.Generic;

namespace OllamaClientLibrary.Abstractions.Tools
{
    public class OllamaProperty
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public List<string>? Enum { get; set; }
    }
}
