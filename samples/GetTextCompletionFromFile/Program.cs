using OllamaClientLibrary;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Models;

using var client = new OllamaClient(new OllamaOptions
{
    Temperature = Temperature.CodingOrMath,
    AssistantBehavior = "Act as an OCR assistant. Analyze the provided image and recognize all visible text in the image as accurately as possible.",
    Model = "llava-phi3" // vision model
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
    Console.WriteLine("Generating text completions from the content of the specified files...");
    
    foreach (var file in files)
    {
        // For text-based files, use local libraries (NPOI) to extract text and then send it to the AI model.
        // For PDF files, use PdfPig to extract text and send it to the AI model. If the PDF is image-based, use OCR to extract text. If text extraction fails, convert the PDF to an image and send it to the AI model.
        // For image-based files, use OCR (Tesseract) to extract text and send it to the AI model. If text extraction fails, send the image directly to the AI model.
        var response = await client.GetTextCompletionFromFileAsync("Please analyze the provided file and recognize all visible text in the file as accurately as possible using Optical Character Recognition (OCR). Additionally, generate a text completion based on the content of the file.", file);

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