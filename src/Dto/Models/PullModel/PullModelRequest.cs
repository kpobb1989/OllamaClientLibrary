namespace OllamaClientLibrary.Dto.Models.PullModel
{
    internal class PullModelRequest
    {
        public string? Model { get; set; }

        public bool Stream { get; set; }
    }
}
