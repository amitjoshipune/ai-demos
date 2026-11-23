using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OllamaSharp;

namespace AiLabs.Chat;

/// <summary>
/// One factory for every lab. The rest of the code only sees <see cref="IChatClient"/>.
/// Cursor is not a provider here — it is the editor. C# talks to Ollama / OpenAI / GitHub / Azure.
/// </summary>
public static class ChatFactory
{
    public static IChatClient Create(LabSettings settings) => settings.Provider switch
    {
        "ollama" => CreateOllama(settings),
        "openai" => CreateOpenAi(settings),
        "github" => CreateGitHub(settings),
        "azure" => CreateAzure(settings),
        "fake" => new DemoChatClient(),
        _ => throw new InvalidOperationException(
            $"Unknown --provider '{settings.Provider}'. Use ollama | openai | github | azure | fake.")
    };

    public static IChatClient CreateWithTools(LabSettings settings) =>
        Create(settings).AsBuilder().UseFunctionInvocation().Build();

    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddings(LabSettings settings) => settings.Embed switch
    {
        "hashbag" or "toy" or "hash" => new Rag.HashBagEmbeddingGenerator(),
        "ollama" => new OllamaApiClient(new Uri(settings.OllamaUrl), settings.EmbedModel),
        "openai" => CreateOpenAiEmbeddings(settings),
        _ => throw new InvalidOperationException("Unknown --embed. Use hashbag | ollama | openai.")
    };

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAiEmbeddings(LabSettings settings)
    {
        var key = Env("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_EMBED_MODEL") ?? "text-embedding-3-small";
        return new OpenAIClient(key).GetEmbeddingClient(model).AsIEmbeddingGenerator();
    }

    private static IChatClient CreateOllama(LabSettings settings) =>
        new OllamaApiClient(new Uri(settings.OllamaUrl), settings.ChatModel);

    private static IChatClient CreateOpenAi(LabSettings settings)
    {
        var key = Env("OPENAI_API_KEY");
        var model = GetOrEnv(settings.ChatModel, "OPENAI_MODEL", "gpt-4o-mini");
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");

        OpenAIClient client = string.IsNullOrWhiteSpace(endpoint)
            ? new OpenAIClient(key)
            : new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateGitHub(LabSettings settings)
    {
        var token = Env("GITHUB_TOKEN");
        var model = GetOrEnv(settings.ChatModel, "GITHUB_MODEL", "openai/gpt-4o-mini");
        var endpoint = Environment.GetEnvironmentVariable("GITHUB_MODELS_ENDPOINT")
            ?? "https://models.github.ai/inference";

        var client = new OpenAIClient(
            new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
        return client.GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateAzure(LabSettings settings)
    {
        var endpoint = Env("AZURE_OPENAI_ENDPOINT");
        var key = Env("AZURE_OPENAI_API_KEY");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(deployment))
            throw new InvalidOperationException("Set AZURE_OPENAI_DEPLOYMENT to your Azure *deployment name* (not the model name).");

        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
            .GetChatClient(deployment)
            .AsIChatClient();
    }

    private static string Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing environment variable '{name}'.");

    private static string GetOrEnv(string current, string envName, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(current) && current != "llama3.2")
            return current;
        return Environment.GetEnvironmentVariable(envName) is { Length: > 0 } value ? value : fallback;
    }
}
