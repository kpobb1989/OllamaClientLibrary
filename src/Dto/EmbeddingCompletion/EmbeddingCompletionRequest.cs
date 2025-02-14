namespace OllamaClientLibrary.Dto.EmbeddingCompletion
{
    internal class EmbeddingCompletionRequest
    {
        public string? Model { get; set; }
        public string[]? Input { get; set; }

        public ModelOptions? Options { get; set; }
    }
}
