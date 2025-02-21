
using OllamaClientLibrary;
using OllamaClientLibrary.Abstractions;
using OllamaClientLibrary.Constants;
using OllamaClientLibrary.Extensions;

using var client = new OllamaClient(new OllamaOptions()
{
    Temperature = Temperature.CreativityWritingOrPoetry
});

var cts = new CancellationTokenSource();

Console.WriteLine("CTRL+C to terminate the conversation.");
#pragma warning disable CS4014
Task.Run(() =>
{
    while (true)
    {
        var keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers == ConsoleModifiers.Control)
        {
            cts.Cancel();
        }
    }
});
#pragma warning restore CS4014

// Load history from the DB if needed
var conversationHistory = new List<OllamaChatMessage>();


while (true)
{
    if (cts.Token.IsCancellationRequested)
    {
        cts = new CancellationTokenSource();
    }

    Console.Write($"{MessageRole.User}: ");

    var prompt = Console.ReadLine()!;

    conversationHistory.Add(prompt.AsUserMessage());

    try
    {
        await foreach (var chunk in client.GetChatCompletionAsync(conversationHistory, ct: cts.Token).Select((message, index) => (Message: message, Index: index)))
        {
            if (chunk.Index == 0)
            {
                Console.Write($"{chunk.Message?.Role}: ");
            }

            Console.Write($"{chunk.Message?.Content}");
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
}