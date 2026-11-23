using AiLabs.Chat;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var settings = LabArgs.Parse(args);
var serverProject = LabArgs.FindRepoFile(Path.Combine("09-mcp-server", "DeskMcp", "DeskMcp.csproj"));

Console.WriteLine("MCP client starting the local C# MCP server over stdio.");
Console.WriteLine("Server: " + serverProject);
Console.WriteLine("The server does not call the LLM. This client can, with --agent.\n");

await using var mcp = await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "desk",
    Command = "dotnet",
    Arguments = ["run", "--project", serverProject]
}));

var tools = await mcp.ListToolsAsync();
Console.WriteLine("Tools the server advertised:");
foreach (var tool in tools)
    Console.WriteLine($"  {tool.Name} — {tool.Description}");

if (!LabArgs.HasFlag(args, "--agent"))
{
    var addName = NameOf(tools, "add");
    var policyName = NameOf(tools, "lookup");
    var add = await mcp.CallToolAsync(addName, new Dictionary<string, object?> { ["a"] = 19, ["b"] = 23 });
    var leave = await mcp.CallToolAsync(policyName, new Dictionary<string, object?> { ["topic"] = "leave" });
    Console.WriteLine("\nDirect calls (no LLM). Names are the server's (often snake_case), not your C# method names.");
    Console.WriteLine($"  {addName}(19,23) => " + TextOf(add));
    Console.WriteLine($"  {policyName}(leave) => " + TextOf(leave));
    Console.WriteLine("\nNext: dotnet run --project 10-mcp-client/DeskMcpClient -- --agent \"How many leave days?\"");
    return;
}

var goal = settings.Question ?? "How many annual leave days do we get?";
Console.WriteLine("\nAgent goal: " + goal);
try
{
    var chat = ChatFactory.CreateWithTools(settings);
    var response = await chat.GetResponseAsync(
        goal,
        new ChatOptions { Tools = [.. tools], Temperature = 0.1f });
    Console.WriteLine("Agent: " + response.Text);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("Need a model for --agent. Or run without --agent to see MCP by itself.");
}

static string TextOf(CallToolResult result) =>
    string.Join(" ", result.Content.OfType<TextContentBlock>().Select(b => b.Text));

static string NameOf(IList<McpClientTool> tools, string fragment) =>
    tools.First(t => t.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)).Name;
