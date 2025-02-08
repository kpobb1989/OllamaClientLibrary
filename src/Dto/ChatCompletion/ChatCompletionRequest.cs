namespace OllamaClientLibrary.Dto.ChatCompletion
{
    internal record ChatCompletionRequest
    {
        public string? Model { get; init; }
        public string? Prompt { get; init; }
        public IEnumerable<ChatMessage>? Messages { get; init; }
        public bool Stream { get; init; }
        public ModelOptions? Options { get; init; }
    }
}
