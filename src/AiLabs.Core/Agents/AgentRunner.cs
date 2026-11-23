using AiLabs.Chat;
using AiLabs.Prompts;
using AiLabs.Tools;
using Microsoft.Extensions.AI;

namespace AiLabs.Agents;

/// <summary>
/// An "agent" here is not a new Microsoft type. It is a chat client + tools + a goal-seeking system prompt.
/// </summary>
public static class AgentRunner
{
    public static async Task<string> RunAsync(
        IChatClient rawClient,
        string goal,
        bool useMiddleware,
        Action<string>? log = null)
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.System, PromptCatalog.Agent),
            new(ChatRole.User, goal)
        };

        var options = new ChatOptions
        {
            Tools = OfficeTools.AsAiTools(),
            Temperature = 0.1f
        };

        if (useMiddleware)
        {
            var client = rawClient.AsBuilder().UseFunctionInvocation().Build();
            var response = await client.GetResponseAsync(history, options);
            log?.Invoke("middleware finished");
            return response.Text ?? "";
        }

        return await ManualToolLoop.RunAsync(rawClient, history, options, log: log);
    }
}
