namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    internal record GenerateCompletionRequest
    {
        public string? Model { get; init; }
        public string? Prompt { get; init; }
        public object? Format { get; init; }
        public bool Stream { get; init; }
        public ModelOptions? Options { get; init; }
    }
}
