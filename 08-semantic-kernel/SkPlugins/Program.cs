using System.ComponentModel;
using AiLabs.Chat;
using AiLabs.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

var settings = LabArgs.Parse(args);
if (settings.Provider == "fake")
{
    Console.WriteLine("Semantic Kernel needs a real model. Rehearse tools with:");
    Console.WriteLine("  dotnet run --project 05-tools/ToolCalling -- --provider fake");
    return;
}

if (settings.Provider is not "ollama")
{
    Console.WriteLine("This lab is Ollama + SK. For Azure at work, swap one line:");
    Console.WriteLine("  builder.AddAzureOpenAIChatCompletion(deployment, endpoint, key);");
    Console.WriteLine("Same plugins. Run the other labs with --provider azure for IChatClient.");
}

#pragma warning disable SKEXP0070
var builder = Kernel.CreateBuilder();
builder.AddOllamaChatCompletion(settings.ChatModel, new Uri(settings.OllamaUrl));
builder.Plugins.AddFromType<OfficePlugin>();
var kernel = builder.Build();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var exec = new OllamaPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

Console.WriteLine("Semantic Kernel: Kernel + plugins. Same GetTime / AddNumbers / policy as lab 05.");
Console.WriteLine("SK is optional. Lab 05 already did this with IChatClient.");
Console.WriteLine("Empty line quits.\n");

var history = new ChatHistory("You help employees. Use plugins. Answer in 2 lines.");
while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;
    history.AddUserMessage(input);
    try
    {
        var reply = await chat.GetChatMessageContentAsync(history, exec, kernel);
        history.Add(reply);
        Console.WriteLine("AI: " + reply.Content + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
        Console.WriteLine("Need llama3.2 via Ollama. Run 00-check.\n");
        history.RemoveAt(history.Count - 1);
    }
}

public sealed class OfficePlugin
{
    [KernelFunction, Description("Get the current local date and time.")]
    public string GetTime() => OfficeTools.GetTime();

    [KernelFunction, Description("Add two integers and return the sum.")]
    public int AddNumbers(int a, int b) => OfficeTools.AddNumbers(a, b);

    [KernelFunction, Description("Look up Contoso policy. Topic should be leave, laptop, or expense.")]
    public string LookupLeavePolicy(string topic) => OfficeTools.LookupLeavePolicy(topic);
}
