﻿using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.ChatCompletion.Tools.Request
{
    internal class Property
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public List<string>? Enum { get; set; }
    }
}
