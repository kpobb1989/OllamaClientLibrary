using Ollama.NET;
using Ollama.NET.Constants;

using var client = new OllamaClient(new LocalOllamaOptions()
{
    Temperature = Temperature.CreativityWritingOrPoetry
});
var cts = new CancellationTokenSource();

Console.WriteLine("CTRL+D to terminate the conversation.");
#pragma warning disable CS4014
Task.Run(() =>
{
    while (true)
    {
        var keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.D && keyInfo.Modifiers == ConsoleModifiers.Control)
        {
            cts.Cancel();
        }
    }
});
#pragma warning restore CS4014

while (true)
{
    if (cts.Token.IsCancellationRequested)
    {
        cts = new CancellationTokenSource();
    }

    Console.Write($"{MessageRole.User}: ");

    var prompt = Console.ReadLine()!;

    try
    {
        await foreach (var chunk in client.GetChatCompletionAsync(prompt, ct: cts.Token).Select((message, index) => (Message: message, Index: index)))
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