using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using var client = new OllamaClient(new OllamaOptions
{
    Temperature = Temperature.CodingOrMath,
    AssistantBehavior = "Act as an OCR assistant. Analyze the provided image and recognize all visible text in the image as accurately as possible.",
    Model = "llava-phi3",
    UseOcrToExtractText = true
});

var path = Path.Combine(Directory.GetCurrentDirectory(), "files");

var files = new OllamaFile[]
{
    new($"{path}/text.doc"),
    new($"{path}/text.docx"),
    new($"{path}/text.xls"),
    new($"{path}/text.xlsx"),
    new($"{path}/text.csv"),
    new($"{path}/text.json"),
    new($"{path}/text.xml"),
    new($"{path}/text.jpg"),
    new($"{path}/text.png"),
    new($"{path}/text.pdf"),
};

try
{
    foreach (var file in files)
    {
        var response = await client.GetTextCompletionFromFileAsync("Get text our of the image", file);

        Console.WriteLine(response);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    foreach (var file in files)
    {
        file?.FileStream?.Dispose();
    }
}

Console.ReadKey();