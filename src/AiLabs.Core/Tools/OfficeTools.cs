using System.ComponentModel;
using AiLabs.Rag;
using Microsoft.Extensions.AI;

namespace AiLabs.Tools;

/// <summary>
/// Ordinary C# methods. Lab 05 passes them as tools. Lab 08 wraps them as SK plugins.
/// Lab 09 exposes them over MCP. Same methods, three hosting styles.
/// </summary>
public static class OfficeTools
{
    [Description("Get the current local date and time.")]
    public static string GetTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    [Description("Add two integers and return the sum.")]
    public static int AddNumbers(
        [Description("First number")] int a,
        [Description("Second number")] int b) => a + b;

    [Description("Look up Contoso policy. Topic should be leave, laptop, or expense.")]
    public static string LookupLeavePolicy(
        [Description("Policy topic: leave, laptop, or expense")] string topic)
    {
        var key = (topic ?? "").Trim().ToLowerInvariant();
        var text = CompanyHandbook.Text;
        if (key.Contains("laptop") || key.Contains("device") || key.Contains("notebook"))
            return Slice(text, "Laptop refresh", "Expenses");
        if (key.Contains("expense") || key.Contains("travel") || key.Contains("receipt"))
            return Slice(text, "Expenses", null);
        return Slice(text, "Annual leave", "Laptop refresh");
    }

    public static IList<AITool> AsAiTools() =>
    [
        AIFunctionFactory.Create(GetTime),
        AIFunctionFactory.Create(AddNumbers),
        AIFunctionFactory.Create(LookupLeavePolicy)
    ];

    private static string Slice(string text, string startHeader, string? nextHeader)
    {
        var startToken = "## " + startHeader;
        var start = text.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return "No matching policy.";
        if (nextHeader is null)
            return text[start..].Trim();
        var end = text.IndexOf("## " + nextHeader, start + startToken.Length, StringComparison.OrdinalIgnoreCase);
        return (end < 0 ? text[start..] : text[start..end]).Trim();
    }
}
