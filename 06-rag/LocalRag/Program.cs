using AiLabs.Chat;
using AiLabs.Rag;
using Microsoft.Extensions.AI;

var settings = LabArgs.Parse(args);
IChatClient chat;
IEmbeddingGenerator<string, Embedding<float>> embedder;
IVectorStore store;
try
{
    chat = ChatFactory.Create(settings);
    embedder = ChatFactory.CreateEmbeddings(settings);
    store = VectorStoreFactory.Create(settings.Store, settings.Connection);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

var rag = new RagPipeline(embedder, store);
await rag.IndexAsync(CompanyHandbook.Text);

Console.WriteLine($"chat={settings.Provider}/{settings.ChatModel}  embed={settings.Embed}  store={store.Name}");
Console.WriteLine("RAG steps: chunk → embed → store → search → stuff hits into the prompt → llama3.2.");
Console.WriteLine("Not needed for RAG: function calling, Semantic Kernel, MCP, agents.");
Console.WriteLine("Try: How many leave days?  |  What is the meal cap?  |  What is the CEO salary?");
Console.WriteLine("Flags: --store memory|sqlite|qdrant|postgres|sqlserver   --embed hashbag|ollama|openai");
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
        {
            var preview = hit.Chunk.Text.Replace("\n", " ");
            if (preview.Length > 90)
                preview = preview[..90];
            Console.WriteLine($"  {hit.Score:0.00}  {preview}");
        }

        Console.WriteLine("AI: " + answer + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        if (settings.Embed == "ollama")
            Console.WriteLine("Need: ollama pull nomic-embed-text\n");
    }
}

if (store is IAsyncDisposable ad)
    await ad.DisposeAsync();
else if (store is IDisposable d)
    d.Dispose();
