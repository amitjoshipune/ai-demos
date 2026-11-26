using AiLabs.Agents;
using AiLabs.Chat;

var settings = LabArgs.Parse(args);
var goal = settings.Question ?? "How many annual leave days do we get, and what time is it now?";

Console.WriteLine($"Provider={settings.Provider}");
Console.WriteLine("Agent = chat + tools + a loop until the goal is done. Still no MCP and no Semantic Kernel.");
Console.WriteLine("Goal: " + goal + "\n");

try
{
    var client = ChatFactory.Create(settings);
    var answer = await AgentRunner.RunAsync(client, goal, useMiddleware: true, log: msg => Console.WriteLine("  " + msg));
    Console.WriteLine("Agent: " + answer);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("Need Ollama? Run 00-check, or rehearse with --provider fake");
}
