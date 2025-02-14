namespace OllamaClientLibrary.Dto.ChatCompletion.Tools.Request
{
    public class Function
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Parameter Parameters { get; set; } = new Parameter();
    }
}
