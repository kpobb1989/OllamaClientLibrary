namespace OllamaClientLibrary
{
    public abstract class OllamaOptions
    {
        public abstract string Model { get; set; }
        public abstract float? Temperature { get; set; }
        public abstract string Host { get; set; }
        public abstract string GenerateApi { get; }
        public abstract string ChatAapi { get; }
        public abstract string TagsApi { get; }
        public abstract string? ApiKey { get; set; }
    }
}
