namespace OllamaClientLibrary.Abstractions
{
    public sealed class OllamaPullModelProgress
    {
        public string? Status { get; set; }
        public double Percentage { get; set; }
    }
}
