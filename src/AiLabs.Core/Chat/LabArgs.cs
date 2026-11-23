namespace AiLabs.Chat;

/// <summary>Command-line flags shared by every lab.</summary>
public sealed record LabSettings(
    string Provider,
    string OllamaUrl,
    string ChatModel,
    string EmbedModel,
    bool RealEmbeddings,
    string? Question);

public static class LabArgs
{
    public static LabSettings Parse(string[] args)
    {
        var provider = Get(args, "--provider")
            ?? Environment.GetEnvironmentVariable("AI_PROVIDER")
            ?? "ollama";

        return new LabSettings(
            Provider: provider.Trim().ToLowerInvariant(),
            OllamaUrl: Get(args, "--ollama-url") ?? "http://localhost:11434",
            ChatModel: Get(args, "--model") ?? "llama3.2",
            EmbedModel: Get(args, "--embed-model") ?? "nomic-embed-text",
            RealEmbeddings: HasFlag(args, "--real-embeddings"),
            Question: FirstPositional(args));
    }

    public static string? Get(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
            if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                return args[i][(name.Length + 1)..];
        }

        return null;
    }

    public static bool HasFlag(string[] args, string name) =>
        args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static readonly HashSet<string> SwitchFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--real-embeddings",
        "--agent"
    };

    public static string? FirstPositional(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (SwitchFlags.Contains(args[i]))
                continue;
            if (!args[i].StartsWith('-'))
                return args[i];
            if (!args[i].Contains('=') && i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                i++; // skip the value of --flag value
        }

        return null;
    }

    /// <summary>Walk up from the running assembly until <paramref name="relative"/> exists.</summary>
    public static string FindRepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find '{relative}' walking up from {AppContext.BaseDirectory}.");
    }
}
