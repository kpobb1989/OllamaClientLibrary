namespace OllamaClientLibrary.Dto.GenerateCompletion
{
    internal class GenerateCompletionRequest
    {
        public string? Model { get; set; }
        public string? Prompt { get; set; }
        public object? Format { get; set; }
        public bool Stream { get; set; }
        public ModelOptions? Options { get; set; }
    }
}
