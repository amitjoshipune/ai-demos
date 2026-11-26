using System.ComponentModel;
using AiLabs.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class DeskTools
{
    [McpServerTool, Description("Get the current local date and time.")]
    public static string GetTime() => OfficeTools.GetTime();

    [McpServerTool, Description("Add two integers and return the sum.")]
    public static int AddNumbers(
        [Description("First number")] int a,
        [Description("Second number")] int b) => OfficeTools.AddNumbers(a, b);

    [McpServerTool, Description("Look up Contoso policy. Topic should be leave, laptop, or expense.")]
    public static string LookupLeavePolicy(
        [Description("Policy topic: leave, laptop, or expense")] string topic) =>
        OfficeTools.LookupLeavePolicy(topic);
}
