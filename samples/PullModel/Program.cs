
using OllamaClientLibrary;
using OllamaClientLibrary.Dto.PullModel;

using var client = new OllamaClient();

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

Console.WriteLine("Enter a model name:");

var prompt = Console.ReadLine()!;

try
{

    var progress = new Progress<PullModelProgress>(progress =>
    {
        Console.WriteLine($"Progress: {Math.Round(progress.Percentage)}% - Status: {progress?.Status}");
    });

    await client.PullModelAsync(prompt, progress, ct: cts.Token);
}
catch (OperationCanceledException)
{
}
catch (Exception ex)

{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.ReadKey();