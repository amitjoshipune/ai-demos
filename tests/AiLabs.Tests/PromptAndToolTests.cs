using AiLabs.Chat;
using AiLabs.Prompts;
using AiLabs.Tools;
using Microsoft.Extensions.AI;

namespace AiLabs.Tests;

public class PromptAndToolTests
{
    [Fact]
    public void Grounded_prompt_tells_the_model_to_say_i_dont_know()
    {
        var system = PromptCatalog.SystemFor(PromptCatalog.Grounded);
        Assert.Contains("I don't know", system, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rag_prompt_includes_context_and_question()
    {
        var prompt = PromptCatalog.Rag("how many leave days?", ["Full-time employees get 20 days of annual leave."]);
        Assert.Contains("20 days", prompt);
        Assert.Contains("how many leave days?", prompt);
        Assert.Contains("I don't know", prompt);
    }

    [Fact]
    public void Office_tools_read_the_handbook()
    {
        Assert.Contains("20 days", OfficeTools.LookupLeavePolicy("leave"));
        Assert.Contains("4 years", OfficeTools.LookupLeavePolicy("laptop"));
        Assert.Contains("40 USD", OfficeTools.LookupLeavePolicy("expense"));
        Assert.Equal(42, OfficeTools.AddNumbers(19, 23));
        Assert.False(string.IsNullOrWhiteSpace(OfficeTools.GetTime()));
    }

    [Fact]
    public async Task Manual_tool_loop_calls_add_on_the_fake_model()
    {
        IChatClient client = new DemoChatClient();
        var history = new List<ChatMessage> { new(ChatRole.User, "Please add 19 and 23") };
        var options = new ChatOptions { Tools = OfficeTools.AsAiTools() };

        var answer = await ManualToolLoop.RunAsync(client, history, options);

        Assert.Contains("42", answer);
        Assert.Contains(history, m => m.Role == ChatRole.Tool);
    }

    [Fact]
    public async Task Manual_tool_loop_looks_up_leave_policy()
    {
        IChatClient client = new DemoChatClient();
        var history = new List<ChatMessage> { new(ChatRole.User, "How many annual leave days?") };
        var options = new ChatOptions { Tools = OfficeTools.AsAiTools() };

        var answer = await ManualToolLoop.RunAsync(client, history, options);

        Assert.Contains("20", answer);
    }
}
