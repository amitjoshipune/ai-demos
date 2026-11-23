using AiLabs.Chat;
using AiLabs.Rag;
using Microsoft.Extensions.AI;

var settings = LabArgs.Parse(args);
IChatClient chat;
IEmbeddingGenerator<string, Embedding<float>> embedder;
try
{
    chat = ChatFactory.Create(settings);
    embedder = ChatFactory.CreateEmbeddings(settings);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

var rag = new RagPipeline(embedder);
await rag.IndexAsync(CompanyHandbook.Text);

Console.WriteLine($"Provider={settings.Provider}  embeddings={(settings.RealEmbeddings ? settings.EmbedModel : "HashBag toy")}");
Console.WriteLine("RAG = Retrieval Augmented Generation (not 'reward').");
Console.WriteLine("llama3.2 cannot embed. Real vectors: ollama pull nomic-embed-text then --real-embeddings");
Console.WriteLine("Try: How many leave days?  |  What is the meal cap?  |  What is the CEO salary?");
Console.WriteLine("Empty line quits.\n");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;

    try
    {
        var (answer, hits) = await rag.AskAsync(chat, input);
        Console.WriteLine("Retrieved:");
        foreach (var hit in hits)
            Console.WriteLine($"  {hit.Score:0.00}  {hit.Chunk.Text.Replace("\n", " ")[..Math.Min(90, hit.Chunk.Text.Length)]}");
        Console.WriteLine("AI: " + answer + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        if (settings.RealEmbeddings)
            Console.WriteLine("Is nomic-embed-text pulled? Check with 00-check.\n");
    }
}
