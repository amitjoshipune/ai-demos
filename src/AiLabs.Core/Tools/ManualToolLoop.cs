using Microsoft.Extensions.AI;

namespace AiLabs.Tools;

/// <summary>
/// The interview version of tool calling, written out as a loop.
/// Production code can instead wrap the client with <c>UseFunctionInvocation()</c>
/// (see <see cref="Chat.ChatFactory.CreateWithTools"/>) — same steps, less typing.
/// </summary>
public static class ManualToolLoop
{
    public static async Task<string> RunAsync(
        IChatClient client,
        IList<ChatMessage> history,
        ChatOptions options,
        int maxRounds = 6,
        Action<string>? log = null)
    {
        for (var round = 1; round <= maxRounds; round++)
        {
            var response = await client.GetResponseAsync(history, options);
            history.AddMessages(response);

            var calls = response.Messages
                .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                .ToList();

            if (calls.Count == 0)
            {
                log?.Invoke($"round {round}: final text");
                return response.Text ?? "";
            }

            foreach (var call in calls)
            {
                log?.Invoke($"round {round}: tool {call.Name}({Format(call.Arguments)})");
                var tool = options.Tools?.OfType<AIFunction>()
                    .FirstOrDefault(t => t.Name.Equals(call.Name, StringComparison.OrdinalIgnoreCase));

                string result;
                if (tool is null)
                {
                    result = $"Unknown tool '{call.Name}'.";
                }
                else
                {
                    var raw = await tool.InvokeAsync(new AIFunctionArguments(call.Arguments ?? new Dictionary<string, object?>()));
                    result = raw?.ToString() ?? "";
                }

                history.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(call.CallId, result)]));
            }
        }

        return "(stopped after max tool rounds)";
    }

    private static string Format(IDictionary<string, object?>? args)
    {
        if (args is null || args.Count == 0)
            return "";
        return string.Join(", ", args.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
