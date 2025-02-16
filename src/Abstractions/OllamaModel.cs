using System;
namespace OllamaClientLibrary.Abstractions
{
    public class OllamaModel
    {
        public string? Name { get; set; }

        public long? Size { get; set; }

        public DateTime? ModifiedAt { get; set; }
    }
}
