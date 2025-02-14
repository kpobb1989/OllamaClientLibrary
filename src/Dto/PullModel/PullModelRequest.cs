namespace OllamaClientLibrary.Dto.PullModel
{
    internal class PullModelRequest
    {
        public string? Model { get; set; }

        public bool Stream { get; set; }
    }
}
