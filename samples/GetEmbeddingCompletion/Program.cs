using OllamaClientLibrary;

using var client = new OllamaClient();

// Step 1: Store a single comprehensive fact
string statement = "I live in California and work as a software engineer at a leading tech company. I have over ten years of experience in the software development industry, specializing in full-stack development. I hold a master's degree in computer science from a prestigious university. In my current role, I lead a team of developers working on innovative projects that leverage cutting-edge technologies such as artificial intelligence, machine learning, and cloud computing. I am passionate about coding, problem-solving, and continuously learning new skills to stay updated with the latest industry trends. Outside of work, I enjoy hiking, reading tech blogs, and contributing to open-source projects.";


List<(double[] Embedding, string Statement)> embeddingsToStatements = [];

Console.WriteLine("Generating embeddings for the statement...");

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
{
    "Where do I live?",
    "What is my profession?",
    "Which state do I live in?",
    "What company do I work for?",
    "How many years of experience do I have in the software development industry?",
    "What is my area of specialization in software development?",
    "What degree do I hold?",
    "From which type of institution did I receive my master's degree?",
    "What is my current role at work?",
    "What kind of projects do I work on?",
    "What technologies do I use in my projects?",
    "What are my passions related to my profession?",
    "What do I enjoy doing outside of work?",
    "What kind of blogs do I read?",
    "What kind of projects do I contribute to outside of work?"
};

List<(double[] Embedding, string Question)> embeddingsToQuestions = [];

Console.WriteLine("Generating embeddings for questions...");

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

Console.ReadKey();

// Method to calculate cosine similarity between two vectors. Ideally, a specialized database should be used for vector comparisons.
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