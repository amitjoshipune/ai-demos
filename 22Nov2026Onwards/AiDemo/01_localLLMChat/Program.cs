using Microsoft.Extensions.AI;
using OllamaSharp;

const string ollamaUrl = "http://192.168.1.6:11434";

const string ollamaModel = "llama3.2:latest";

IChatClient client = new OllamaApiClient(new Uri(ollamaUrl), ollamaModel);

var history = new List<ChatMessage>
{
    new (ChatRole.System, "You are a helpful assistant. Answer in 2-3 short sentences."),
    new (ChatRole.User, "What is the capital of France?"),
    new (ChatRole.Assistant, "The capital of France is Paris. It is known for its art, culture, and history.")
};

Dump("about to send this to llama3.2", history);

try
{
    ChatResponse response = await client.GetResponseAsync(history);

    string text = response?.Text ?? "";

    Console.WriteLine();
    Console.WriteLine($"AI [{ChatRole.Assistant}]: {text}");
    Console.WriteLine();
    history.Add(new ChatMessage(ChatRole.Assistant,text));
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
    Console.WriteLine("Is Ollama running?  ollama list   then try the URL above.");
    history.RemoveAt(history.Count - 1);
}


static void Dump(string title, List<ChatMessage> history)
{
    Console.WriteLine($"--- {title} ---");
    for (int i = 0; i < history.Count; i++)
    {
        ChatMessage m = history[i];
        string preview = (m.Text ?? "").Replace("\n", " ");
        if (preview.Length > 80)
            preview = preview[..80] + "...";
        Console.WriteLine($"  {i}: [{m.Role}] {preview}");
    }
    Console.WriteLine("----------------------------");
}