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
    Console.WriteLine("Extracting text from the specified files using Optical Character Recognition (OCR)...");

    foreach (var file in files)
    {
        // For text-based files, extract the text using local libraries
        // For image-based files, use OCR (Tesseract) to extract text from the image
        // For PDF files: extract text from the PDF using PdfPig, if the pdf is image-based, use OCR (Tesseract) to extract text from the image
        var response = await client.GetOcrTextFromFileAsync(file);

        Console.WriteLine(response);
    }
    
    Console.WriteLine("Generating text completions from the content of the specified files...");
    
    foreach (var file in files)
    {
        // For text-based files, extract text using local libraries and then send the text to the AI model
        // For image-based files, send the image directly to the AI model (make sure the model supports image input)
        // For PDF files, convert to image first and then send the image to the AI model (make sure the model supports image input)
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