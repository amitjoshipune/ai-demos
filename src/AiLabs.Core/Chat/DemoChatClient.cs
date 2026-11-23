using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace AiLabs.Chat;

/// <summary>
/// Offline stand-in for a chat model. Use <c>--provider fake</c> to rehearse
/// tool calling, RAG, and agents without Ollama. It is not an LLM.
/// </summary>
public sealed class DemoChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var lastUser = list.FindLastIndex(m => m.Role == ChatRole.User);
        var sinceUser = lastUser >= 0 ? list.Skip(lastUser + 1) : list.AsEnumerable();
        var toolResults = sinceUser
            .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
            .Select(r => r.Result?.ToString() ?? "")
            .Where(s => s.Length > 0)
            .ToList();

        if (toolResults.Count > 0)
        {
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "From tools: " + string.Join("; ", toolResults))));
        }

        var last = list.LastOrDefault()?.Text ?? "";
        var tools = options?.Tools?.OfType<AIFunction>().ToList() ?? [];

        if (tools.Count > 0 && TryToolCalls(last, tools) is { Count: > 0 } calls)
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, calls)));

        if (last.Contains("Context:", StringComparison.OrdinalIgnoreCase))
        {
            var question = QuestionOf(last);
            string grounded;
            if (LooksUnknown(question))
                grounded = "I don't know — that is not in the context.";
            else if (question.Contains("leave", StringComparison.OrdinalIgnoreCase)
                     && last.Contains("20 days", StringComparison.OrdinalIgnoreCase))
                grounded = "The handbook says employees get 20 days of annual leave.";
            else if (question.Contains("meal", StringComparison.OrdinalIgnoreCase)
                     || question.Contains("expense", StringComparison.OrdinalIgnoreCase))
                grounded = last.Contains("40 USD", StringComparison.OrdinalIgnoreCase)
                    ? "The handbook caps travel meals at 40 USD per day."
                    : "I don't know — that is not in the context.";
            else if (question.Contains("laptop", StringComparison.OrdinalIgnoreCase))
                grounded = last.Contains("4 years", StringComparison.OrdinalIgnoreCase)
                    ? "The handbook says laptops are refreshed every 4 years."
                    : "I don't know — that is not in the context.";
            else
                grounded = "I don't know — that is not in the context.";

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, grounded)));
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            "Fake model. Ask about time, leave policy, laptop refresh, expenses, or adding numbers.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text)
        {
            FinishReason = response.FinishReason
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
            return new ChatClientMetadata("demo", new Uri("http://demo.local"), "demo");
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
    }

    private static List<AIContent> TryToolCalls(string text, List<AIFunction> tools)
    {
        string? Actual(string pascal) => tools
            .Select(t => t.Name)
            .FirstOrDefault(n => n.Equals(pascal, StringComparison.OrdinalIgnoreCase)
                || n.Replace("_", "").Equals(pascal, StringComparison.OrdinalIgnoreCase));

        var calls = new List<AIContent>();

        if (Actual("AddNumbers") is { } addName && (text.Contains("add", StringComparison.OrdinalIgnoreCase)
            || text.Contains("plus", StringComparison.OrdinalIgnoreCase)
            || text.Contains("sum", StringComparison.OrdinalIgnoreCase)))
        {
            var nums = Regex.Matches(text, @"-?\d+").Select(m => int.Parse(m.Value)).ToList();
            var a = nums.ElementAtOrDefault(0);
            var b = nums.ElementAtOrDefault(1);
            calls.Add(new FunctionCallContent("call-add", addName, new Dictionary<string, object?> { ["a"] = a, ["b"] = b }));
        }

        if (Actual("LookupLeavePolicy") is { } policyName && LooksLikePolicy(text))
        {
            var topic = text.Contains("laptop", StringComparison.OrdinalIgnoreCase) ? "laptop"
                : text.Contains("expense", StringComparison.OrdinalIgnoreCase) ? "expense"
                : "leave";
            calls.Add(new FunctionCallContent("call-policy", policyName, new Dictionary<string, object?> { ["topic"] = topic }));
        }

        if (Actual("GetTime") is { } timeName && (text.Contains("time", StringComparison.OrdinalIgnoreCase)
            || text.Contains("clock", StringComparison.OrdinalIgnoreCase)))
        {
            calls.Add(new FunctionCallContent("call-time", timeName, new Dictionary<string, object?>()));
        }

        return calls;
    }

    private static string QuestionOf(string prompt)
    {
        var i = prompt.LastIndexOf("Question:", StringComparison.OrdinalIgnoreCase);
        return i < 0 ? prompt : prompt[(i + "Question:".Length)..].Trim();
    }

    private static bool LooksUnknown(string question) =>
        question.Contains("salary", StringComparison.OrdinalIgnoreCase)
        || question.Contains("ceo", StringComparison.OrdinalIgnoreCase)
        || question.Contains("bonus", StringComparison.OrdinalIgnoreCase)
        || question.Contains("secret", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePolicy(string text) =>
        text.Contains("leave", StringComparison.OrdinalIgnoreCase)
        || text.Contains("holiday", StringComparison.OrdinalIgnoreCase)
        || text.Contains("pto", StringComparison.OrdinalIgnoreCase)
        || text.Contains("vacation", StringComparison.OrdinalIgnoreCase)
        || text.Contains("laptop", StringComparison.OrdinalIgnoreCase)
        || text.Contains("expense", StringComparison.OrdinalIgnoreCase)
        || text.Contains("policy", StringComparison.OrdinalIgnoreCase);
}
