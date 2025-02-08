namespace OllamaClientLibrary.Dto.Models
{
    internal record ModelResponse
    {
        public IEnumerable<Model> Models { get; init; } = [];
    }
}
