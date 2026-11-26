using System.Net.Http.Json;
using System.Text.Json;

namespace AiLabs.Rag;

/// <summary>
/// Famous dedicated vector DB. Talks HTTP to Qdrant (default http://localhost:6333).
/// Start it with: docker run -p 6333:6333 qdrant/qdrant
/// </summary>
public sealed class QdrantVectorStore : IVectorStore, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _collection;
    private int _dimensions;

    public QdrantVectorStore(string url, string collection = "handbook")
    {
        _collection = collection;
        _http = new HttpClient { BaseAddress = new Uri(url.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(15) };
    }

    public string Name => $"qdrant:{_http.BaseAddress}{_collection}";

    public async Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        _dimensions = dimensions;
        await PutCollection(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using var delete = await _http.DeleteAsync($"collections/{_collection}", cancellationToken);
        if ((int)delete.StatusCode == 404)
            return;
        if (!delete.IsSuccessStatusCode)
            await EnsureOk(delete, "delete collection");
        if (_dimensions > 0)
            await PutCollection(cancellationToken);
    }

    public async Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            points = new[]
            {
                new
                {
                    id = PointId(chunk.Id),
                    vector = vector.ToArray(),
                    payload = new { chunkId = chunk.Id, text = chunk.Text }
                }
            }
        };
        using var response = await _http.PutAsJsonAsync($"collections/{_collection}/points?wait=true", body, cancellationToken);
        await EnsureOk(response, "upsert points");
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default)
    {
        var body = new { vector = query.ToArray(), limit = topK, with_payload = true };
        using var response = await _http.PostAsJsonAsync($"collections/{_collection}/points/search", body, cancellationToken);
        var json = await EnsureOk(response, "search");
        using var doc = JsonDocument.Parse(json);
        var hits = new List<ScoredChunk>();
        foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
        {
            var payload = item.GetProperty("payload");
            hits.Add(new ScoredChunk(
                new TextChunk(payload.GetProperty("chunkId").GetString() ?? "", payload.GetProperty("text").GetString() ?? ""),
                item.GetProperty("score").GetSingle()));
        }

        return hits;
    }

    private async Task PutCollection(CancellationToken cancellationToken)
    {
        var body = new { vectors = new { size = _dimensions, distance = "Cosine" } };
        using var response = await _http.PutAsJsonAsync($"collections/{_collection}", body, cancellationToken);
        await EnsureOk(response, "create collection");
    }

    private static async Task<string> EnsureOk(HttpResponseMessage response, string what)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
            return json;
        throw new InvalidOperationException(
            $"Qdrant {what} failed {(int)response.StatusCode}: {json}\nStart Qdrant: docker run -p 6333:6333 qdrant/qdrant");
    }

    private static int PointId(string chunkId)
    {
        var digits = new string(chunkId.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var n) && n > 0)
            return n;
        return (int)(VectorCodec.StableHash(chunkId) % 1_000_000_007) + 1;
    }

    public void Dispose() => _http.Dispose();
}
