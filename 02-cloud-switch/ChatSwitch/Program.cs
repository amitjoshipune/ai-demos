using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;

// ── 1. Configuration: settings from appsettings.json, the token from user-secrets ──
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

// ── 2. Provider: command line wins, then appsettings.json, then Ollama ──
//      dotnet run            -> local Ollama
//      dotnet run -- GitHub  -> cloud GitHub Models
string provider = (args.FirstOrDefault() ?? config["Provider"] ?? "Ollama").Trim();
bool useGitHub = provider.Equals("GitHub", StringComparison.OrdinalIgnoreCase);

// ── 3. Create ONE client, typed as the interface. Fail fast on missing settings ──
IChatClient client;
try
{
    client = useGitHub
        ? new GitHubModelsChatClient(
              Require(config, "GitHubModels:Endpoint"),
              Require(config, "GitHubModels:Token"),
              Require(config, "GitHubModels:Model"))
        : new OllamaApiClient(new Uri(Require(config, "Ollama:Url")), Require(config, "Ollama:Model"));
}
catch (Exception ex)
{
    Console.WriteLine($"Startup failed: {ex.Message}");
    return;
}

Console.WriteLine($"Provider: {provider}. Ask a question, or press Enter on an empty line to quit.\n");

// ── 4. Context: system rules + history (the model remembers nothing by itself) ──
List<ChatMessage> history =
[
    new(ChatRole.System,
        $"You are a helpful assistant running via {provider}. " +
        "If you are not sure, say 'I don't know'. Answer in at most 5 lines.")
];

// ── 5. Options: low randomness, capped answer length ──
var options = new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 400 };

// ── 6. Chat loop: protected call, streaming output, measure time ──
while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    history.Add(new(ChatRole.User, input));
    var sw = Stopwatch.StartNew();
    List<ChatResponseUpdate> updates = [];

    try
    {
        Console.Write("AI: ");
        await foreach (var update in client.GetStreamingResponseAsync(history, options))
        {
            Console.Write(update.Text);
            updates.Add(update);
        }
        history.AddMessages(updates);               // keep the answer as context for the next question
        Console.WriteLine($"\n[{provider}, {sw.ElapsedMilliseconds} ms]\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError from {provider}: {ex.Message}\n");
        history.RemoveAt(history.Count - 1);         // drop the failed question
    }
}

// ───────────────────────── Helpers ─────────────────────────

// Read a required setting or stop with a message that says exactly what to fix.
static string Require(IConfiguration config, string key) =>
    string.IsNullOrWhiteSpace(config[key])
        ? throw new InvalidOperationException($"Missing setting '{key}'. Check appsettings.json or user-secrets.")
        : config[key]!;

// ───────────── Our own IChatClient for GitHub Models (plain REST) ─────────────
// Calls the OpenAI-compatible endpoint:  POST {endpoint}/chat/completions
sealed class GitHubModelsChatClient : IChatClient
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _model;

    public GitHubModelsChatClient(string endpoint, string token, string model)
    {
        _url = endpoint.TrimEnd('/') + "/chat/completions";
        _model = model;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // 1. Build the request body in the OpenAI chat format
        var body = new Dictionary<string, object?>
        {
            ["model"] = _model,
            ["messages"] = messages.Select(m => new { role = m.Role.Value, content = m.Text }).ToArray()
        };
        if (options?.Temperature is float temperature) body["temperature"] = temperature;
        if (options?.MaxOutputTokens is int maxTokens) body["max_tokens"] = maxTokens;

        // 2. Send it; on failure, show the server's own error text
        using var response = await _http.PostAsJsonAsync(_url, body, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GitHub Models returned {(int)response.StatusCode}: {json}");

        // 3. Read: choices[0].message.content, finish_reason, usage
                // DEBUG: if the server did not send JSON, show the URL and what it did send
        if (!json.TrimStart().StartsWith('{'))
            throw new InvalidOperationException(
                $"Not JSON. URL: {_url} | Status: {(int)response.StatusCode} | " +
                $"Content-Type: {response.Content.Headers.ContentType} | Body: {json[..Math.Min(json.Length, 300)]}");

        using var doc = JsonDocument.Parse(json);

        var choice = doc.RootElement.GetProperty("choices")[0];
        var text = choice.GetProperty("message").GetProperty("content").GetString() ?? "";

        var result = new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { ModelId = _model };
        if (choice.TryGetProperty("finish_reason", out var finish) && finish.ValueKind == JsonValueKind.String)
            result.FinishReason = new ChatFinishReason(finish.GetString()!);
        if (doc.RootElement.TryGetProperty("usage", out var usage))
            result.Usage = new UsageDetails
            {
                InputTokenCount = usage.GetProperty("prompt_tokens").GetInt64(),
                OutputTokenCount = usage.GetProperty("completion_tokens").GetInt64(),
                TotalTokenCount = usage.GetProperty("total_tokens").GetInt64()
            };
        return result;
    }

    // Simple version: gets the full answer, then returns it as one "stream" chunk.
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text)
        {
            FinishReason = response.FinishReason,
            ModelId = _model
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() => _http.Dispose();
}