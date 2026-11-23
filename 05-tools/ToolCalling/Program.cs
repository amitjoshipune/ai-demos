using AiLabs.Chat;
using AiLabs.Tools;
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

var options = new ChatOptions { Tools = OfficeTools.AsAiTools(), Temperature = 0.1f };
var history = new List<ChatMessage>
{
    new(ChatRole.System, "You help employees. Use tools for time, math, and Contoso policy. Then answer in 2 lines.")
};

Console.WriteLine($"Provider={settings.Provider}. This lab is tool calling WITHOUT Semantic Kernel and WITHOUT MCP.");
Console.WriteLine("Try: What time is it?  |  Add 19 and 23  |  How many leave days do we get?");
Console.WriteLine("Rehearse with no model: --provider fake");
Console.WriteLine("Empty line quits.\n");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;

    history.Add(new ChatMessage(ChatRole.User, input));
    try
    {
        var answer = await ManualToolLoop.RunAsync(client, history, options, log: msg => Console.WriteLine("  " + msg));
        Console.WriteLine("AI: " + answer + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        history.RemoveAt(history.Count - 1);
    }
}
