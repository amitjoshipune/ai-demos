using System.Text.Json;

var url = args.FirstOrDefault() ?? "http://localhost:11434";
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

Console.WriteLine($"Checking Ollama at {url}");
Console.WriteLine("Ollama = the server. llama3.2 = a chat model. nomic-embed-text = an embedding model.");
Console.WriteLine("Cursor is the editor. C# cannot call Cursor as a chat API.\n");

try
{
    using var response = await http.GetAsync($"{url.TrimEnd('/')}/api/tags");
    response.EnsureSuccessStatusCode();
    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var models = doc.RootElement.GetProperty("models")
        .EnumerateArray()
        .Select(m => m.GetProperty("name").GetString() ?? "")
        .Where(n => n.Length > 0)
        .ToList();

    if (models.Count == 0)
    {
        Console.WriteLine("Ollama is up but no models are pulled.");
        Console.WriteLine("  ollama pull llama3.2");
        Console.WriteLine("  ollama pull nomic-embed-text");
        return;
    }

    Console.WriteLine("Pulled models:");
    foreach (var name in models)
        Console.WriteLine("  " + name);

    Report("chat model llama3.2", models, "llama3.2", "ollama pull llama3.2");
    Report("embedding model nomic-embed-text", models, "nomic-embed-text", "ollama pull nomic-embed-text");
}
catch (Exception ex)
{
    Console.WriteLine("Ollama is not reachable: " + ex.Message);
    Console.WriteLine("Start it, then: ollama list");
}

static void Report(string label, IReadOnlyList<string> models, string needle, string pull)
{
    var found = models.Any(n => n.StartsWith(needle, StringComparison.OrdinalIgnoreCase));
    Console.WriteLine(found
        ? $"OK  {label} is present."
        : $"NO  {label} missing. Run: {pull}");
}
