
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using System.Text;
using Ollama.NET.Extensions;
using Ollama.NET.Constants;
using Ollama.NET;
using Ollama.NET.Dto;
using Ollama.NET.Dto.ChatCompletion;

using var client = new OllamaClient(new DeepSeekOptions("sk-2fcac6562b8143ceb3e20b17c6604a3e"));

await foreach (var chunk in client.GetChatCompletionAsync("Hi").Select((message, index) => (Message: message, Index: index)))
{
    if (chunk.Index == 0)
    {
        Console.Write($"{chunk.Message?.Role}: ");
    }

    Console.Write($"{chunk.Message?.Content}");
}

Console.ReadKey();

//foreach (var model in await client.ListModelsAsync(model: "deepseek-r1"))
//{
//    Console.WriteLine(model.Name);
//}

//var doc = new StringBuilder();

//using (PdfDocument document = PdfDocument.Open(@"D:\bp-annual-report-and-form-20f-2023.pdf"))
//{
//    foreach (Page page in document.GetPages())
//    {
//        string pageText = page.Text;

//        doc.Append(pageText);
//    }
//}



//var result = await client.GenerateCompletionTextAsync($"What's the name of the model", new OllamaOptions() { Temperature = Temperature.GeneralConversationOrTranslation });

//Console.ReadKey();


//Console.WriteLine("CTRL+D to terminate the conversation.");

//var cts = new CancellationTokenSource();

//// Start a task to monitor for CTRL+D key press
//Task.Run(() =>
//{
//    while (true)
//    {
//        var keyInfo = Console.ReadKey(intercept: true);
//        if (keyInfo.Key == ConsoleKey.D && keyInfo.Modifiers == ConsoleModifiers.Control)
//        {
//            cts.Cancel();
//        }
//    }
//});

//while (true)
//{
//    if (cts.Token.IsCancellationRequested)
//    {
//        cts = new CancellationTokenSource();
//    }

//    Console.Write($"{MessageRole.User}: ");

//    var prompt = Console.ReadLine();

//    await foreach (var chunk in client.GetChatCompletionAsync(prompt.AsUserChatMessage(), ct: cts.Token).Select((message, index) => (Message: message, Index: index)))
//    {
//        if (chunk.Index == 0)
//        {
//            Console.Write($"{chunk.Message?.Role}: ");
//        }

//        Console.Write($"{chunk.Message?.Content}");
//    }

//    Console.WriteLine();
//}