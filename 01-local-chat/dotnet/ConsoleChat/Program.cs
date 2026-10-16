//Console.WriteLine("Hello, World!");
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient client = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");
List<ChatMessage> history = [new(ChatRole.System, "You are a helpful assistant. Answer in 3 lines.")];

Console.WriteLine("Press Q to discontinue...");
    
if (char.ToUpper(Console.ReadKey().KeyChar) == 'Q')
{
    Console.WriteLine("Exiting...");
    Environment.Exit(0);
}

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;
    try
    {
        
    
    history.Add(new(ChatRole.User, input));

    var response = await client.GetResponseAsync(history);
    Console.WriteLine($"AI: {response.Text}");
    history.AddMessages(response);
    }
   catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}  (Is the model pulled? Run: ollama list)");
    }
    
    Console.WriteLine("Press Q to discontinue...");
    
    if (char.ToUpper(Console.ReadKey().KeyChar) == 'Q')
        break;
    else
        Console.WriteLine("Enter your question please");
}
