namespace Ollama.NET.Dto.Models
{
    internal record ModelResponse
    {
        public IEnumerable<Model> Models { get; init; } = [];
    }
}
