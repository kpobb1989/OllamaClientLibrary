namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    public record GenerateCompletionResponse
    {
        public string? Response { get; init; }

        public string? Model { get; init; }
    }
}
