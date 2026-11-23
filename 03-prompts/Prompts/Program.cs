using AiLabs.Chat;
using AiLabs.Prompts;
using Microsoft.Extensions.AI;

var settings = LabArgs.Parse(args);
IChatClient client;
try
{
    client = ChatFactory.Create(settings);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}

Console.WriteLine($"Provider={settings.Provider}  model={settings.ChatModel}");
Console.WriteLine("Styles: chat | json | fewshot | grounded");
Console.WriteLine("Try grounded with: What is our secret bonus policy?");
Console.WriteLine("Empty line quits.\n");

var style = LabArgs.Get(args, "--style") ?? PromptCatalog.Chat;

while (true)
{
    Console.Write($"[{style}] You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;
    if (input.StartsWith("style ", StringComparison.OrdinalIgnoreCase))
    {
        style = input[6..].Trim();
        Console.WriteLine("System prompt is now:\n" + PromptCatalog.SystemFor(style) + "\n");
        continue;
    }

    try
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, PromptCatalog.SystemFor(style)),
            new(ChatRole.User, input)
        };
        var response = await client.GetResponseAsync(history, new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 300 });
        Console.WriteLine("AI: " + response.Text + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        Console.WriteLine("Is Ollama running? Try: dotnet run --project 00-check/CheckOllama\n");
    }
}
