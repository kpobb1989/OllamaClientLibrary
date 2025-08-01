using OllamaClientLibrary;
using OllamaClientLibrary.Models;
using OllamaClientLibrary.Tools;
using System.ComponentModel;
using Newtonsoft.Json;

// 1. Define the schema with descriptions and reference values
var schema = new Dictionary<string, object>
{
    ["CompanyProfile"] = new
    {
        Description = "Contains basic information about companies.",
        Columns = new Dictionary<string, object>
        {
            ["Ticker"] = new { Description = "The stock ticker symbol for the company.", ReferenceValue = "CVX" },
            ["Name"] = new { Description = "The official name of the company.", ReferenceValue = "Chevron" },
            ["Industry"] = new { Description = "The industry sector the company operates in.", ReferenceValue = "Energy" }
        }
    },
    ["CompanyFinancials"] = new
    {
        Description = "Contains financial data for companies.",
        Columns = new Dictionary<string, object>
        {
            ["Revenue"] = new { Description = "Total revenue for the company.", ReferenceValue = "200000000000" },
            ["Profit"] = new { Description = "Net profit for the company.", ReferenceValue = "15000000000" },
            ["Year"] = new { Description = "The fiscal year for the financial data.", ReferenceValue = "2024" }
        }
    }
};

// 2. Inject the schema into the system prompt
string systemPrompt = $@"
You have access to the following database schema (JSON, with descriptions and reference values):
{JsonConvert.SerializeObject(schema)}

For every user question:
- You must build an SQL query to find the requested information using the schema and user input.
- You must call the SqlRunner.RunSqlQuery tool for every request, passing the SQL query you constructed.
- Your response to the user must be a normal, conversational answer and must NOT contain the SQL query itself, only the result of the query.
- If you cannot determine the table, column, or value from the question, ask the user for clarification before proceeding.
- If the question is not related to database knowledge, respond with: ""Sorry, I can not answer this question.""
";

using var client = new OllamaClient(new OllamaOptions()
{
    Model = "qwen3:4b",
    AutoInstallModel = false,
    Tools = ToolFactory.Create<SqlRunnerTool>(),
    ThinkingEnabled = false,
    KeepConversationHistory = false,
    SystemPrompt = systemPrompt
});

while (true)
{
    Console.Write("Enter your question: ");

    var prompt = Console.ReadLine();

    var resp = await client.GetTextCompletionAsync(prompt);

    Console.WriteLine(resp);
}

class SqlRunnerTool
{
    [Description("Executes the provided SQL query against the database and returns the requested value(s).")]
    public string RunSqlQuery(
        [Description("The SQL query to execute.")] string sqlQuery)
    {
        // Stub: Replace with actual DB execution logic.
        // Example: If the query requests Chevron's ticker, return "CVX".
        if (sqlQuery.Contains("SELECT Ticker FROM CompanyProfile WHERE Name = 'Chevron'", StringComparison.OrdinalIgnoreCase))
            return "CVX";

        if (sqlQuery.Contains("SELECT Ticker FROM CompanyProfile WHERE Name = 'Shell'", StringComparison.OrdinalIgnoreCase))
            return "SHEL";

        // You would normally connect to the DB, execute the query, and return results.
        return "Query executed. (Stub: No real DB connection implemented.)";
    }
}