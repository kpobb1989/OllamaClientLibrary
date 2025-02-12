using OllamaClientLibrary;

using var client = new OllamaClient(new OllamaOptions()
{
    Model = "llama3.1:8b" // Ensure this model is available
});

// Step 1: Store a single comprehensive fact
string statement = "I live in California and work as a software engineer.";

List<(double[] Embedding, string Statement)> embeddingsToStatements = [];

Console.WriteLine("Generating embedding for the statement...");

try
{
    double[][] embeddings = await client.GetEmbeddingAsync([statement]);
    if (embeddings.Length != 1)
    {
        Console.WriteLine("Error: Number of embeddings does not match number of statements.");
        return;
    }

    embeddingsToStatements.Add((embeddings[0], statement));
}
catch (Exception ex)
{
    Console.WriteLine($"Error generating stored embedding: {ex.Message}");
    return;
}

// Step 2: Generate embeddings for multiple questions
string[] questions =
[
            "Do I work in California?",
            "Do I live in the United States?",
            "Do I work as a developer?"
];

List<(double[] Embedding, string Question)> embeddingsToQuestions = [];

Console.WriteLine("\nGenerating embeddings for questions...");

try
{
    double[][] questionEmbeddings = await client.GetEmbeddingAsync(questions);

    for (int i = 0; i < questions.Length; i++) // Correct loop condition
    {
        embeddingsToQuestions.Add((questionEmbeddings[i], questions[i]));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error generating question embeddings: {ex.Message}");
    return;
}

// Step 3: Compare embeddings and generate answers
foreach (var (Embedding, Question) in embeddingsToQuestions)
{
    double maxSimilarity = 0;
    string bestMatch = "I am not sure.";

    Console.WriteLine($"\nProcessing Question: {Question}");

    foreach (var statementEntry in embeddingsToStatements)
    {
        double similarity = CosineSimilarity(statementEntry.Embedding, Embedding);

        if (similarity > maxSimilarity)
        {
            maxSimilarity = similarity;
            bestMatch = statementEntry.Statement;
        }
    }

    Console.WriteLine($"\nQuestion: {Question}");
    Console.WriteLine($"Best Matched Fact: {bestMatch}");
    Console.WriteLine($"Cosine Similarity: {maxSimilarity:F4}");

    // Step 4: Answer based on similarity and call deepseek for corrections
    if (maxSimilarity > 0.6)
    {
        // Generate a refined answer using the model with the enhanced prompt
        string? refinedAnswer = await GenerateRefinedAnswer(client, bestMatch, Question);
        Console.WriteLine($"Answer: {refinedAnswer}");
    }
    else
    {
        Console.WriteLine("Answer: I am not sure.");
    }
}

Console.WriteLine("\nProcessing complete. Press any key to exit.");
Console.ReadKey();

// Method to calculate cosine similarity between two vectors
static double CosineSimilarity(double[] vectorA, double[] vectorB)
{
    if (vectorA.Length != vectorB.Length)
        throw new ArgumentException("Vectors must have the same length.");

    double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;

    for (int i = 0; i < vectorA.Length; i++)
    {
        dotProduct += vectorA[i] * vectorB[i];
        magnitudeA += vectorA[i] * vectorA[i];
        magnitudeB += vectorB[i] * vectorB[i];
    }

    magnitudeA = Math.Sqrt(magnitudeA);
    magnitudeB = Math.Sqrt(magnitudeB);

    return (magnitudeA == 0 || magnitudeB == 0) ? 0 : dotProduct / (magnitudeA * magnitudeB);
}

// Method to generate a refined answer using the deepseek model with enhanced prompt
static async Task<string?> GenerateRefinedAnswer(OllamaClient client, string statement, string question)
{
    try
    {
        // Enhanced prompt structure
        string prompt = $"Given the information below, provide a concise answer to the question.\n\n" +
                        $"Information:\n{statement}\n\n" +
                        $"Question:\n{question}\n\n" +
                        $"Answer:";

        return await client.GenerateTextCompletionAsync(prompt);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating refined answer: {ex.Message}");
        return "I am not sure.";
    }
}