using System.Collections.Generic;

namespace OllamaClientLibrary.Dto.Models
{
    internal class ModelResponse
    {
        public IEnumerable<Model> Models { get; set; } = new List<Model>();
    }
}
