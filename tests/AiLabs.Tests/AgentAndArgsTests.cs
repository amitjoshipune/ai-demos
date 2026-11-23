using AiLabs.Agents;
using AiLabs.Chat;

namespace AiLabs.Tests;

public class AgentAndArgsTests
{
    [Fact]
    public void Parse_provider_and_flags()
    {
        var settings = LabArgs.Parse(["--provider", "fake", "--real-embeddings", "What time is it?"]);
        Assert.Equal("fake", settings.Provider);
        Assert.True(settings.RealEmbeddings);
        Assert.Equal("What time is it?", settings.Question);
    }

    [Fact]
    public void Unknown_provider_throws_with_the_list()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ChatFactory.Create(LabArgs.Parse(["--provider", "cursor"])));
        Assert.Contains("fake", ex.Message);
        Assert.DoesNotContain("cursor is a valid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Agent_with_fake_model_uses_tools()
    {
        IChatClient client = new DemoChatClient();
        var answer = await AgentRunner.RunAsync(client, "Add 10 plus 5 and tell me the leave policy", useMiddleware: false);
        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Contains("15", answer);
    }
}
