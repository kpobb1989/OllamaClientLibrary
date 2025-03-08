namespace OllamaClientLibrary.Models.Tools
{
    public class OllamaFunction
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public OllamaParameter Parameters { get; set; } = new OllamaParameter();
    }
}
