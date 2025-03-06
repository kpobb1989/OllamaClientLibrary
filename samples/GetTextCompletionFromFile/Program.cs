using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using var client = new OllamaClient(new OllamaOptions
{
    Temperature = Temperature.CodingOrMath,
    AssistantBehavior = "Act as an OCR assistant. Analyze the provided image and recognize all visible text in the image as accurately as possible.",
    Model = "llava-phi3"
});

Console.Write("Loading...");

try
{
    var file = new OllamaFile(@"D:\1.jpg");

    var response = await client.GetTextFromFileAsync("Get text our of the image", file);

    Console.Clear();

    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();