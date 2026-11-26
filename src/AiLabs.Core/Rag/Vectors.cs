using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace AiLabs.Rag;

public static class Cosine
{
    public static float Similarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
            throw new ArgumentException("Vectors must be the same non-zero length.");

        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom < 1e-12 ? 0 : (float)(dot / denom);
    }
}

public sealed record TextChunk(string Id, string Text);

public static class TextChunker
{
    public static IReadOnlyList<TextChunk> Split(string document, int maxChars = 420)
    {
        var parts = Regex.Split(document.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0 && !p.StartsWith("# "))
            .ToList();

        var chunks = new List<TextChunk>();
        var n = 0;
        foreach (var part in parts)
        {
            if (part.Length <= maxChars)
            {
                chunks.Add(new TextChunk($"c{++n}", part));
                continue;
            }

            for (var i = 0; i < part.Length; i += maxChars)
            {
                var len = Math.Min(maxChars, part.Length - i);
                chunks.Add(new TextChunk($"c{++n}", part.Substring(i, len).Trim()));
            }
        }

        return chunks;
    }
}

/// <summary>
/// Tiny bag-of-words embedder so RAG works before you download nomic-embed-text.
/// Similar words overlap; this is not a production embedding model.
/// </summary>
public sealed class HashBagEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int Dimensions = 64;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = values.Select(v => new Embedding<float>(Embed(v))).ToList();
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    public static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9]+"))
        {
            if (m.Length < 2)
                continue;
            var slot = (int)(VectorCodec.StableHash(m.Value) % (uint)Dimensions);
            vector[slot] += 1f;
        }

        double n = 0;
        foreach (var x in vector)
            n += x * x;
        n = Math.Sqrt(n);
        if (n > 0)
        {
            for (var i = 0; i < vector.Length; i++)
                vector[i] = (float)(vector[i] / n);
        }

        return vector;
    }
}

public sealed record ScoredChunk(TextChunk Chunk, float Score);

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<(TextChunk Chunk, float[] Vector)> _items = [];

    public string Name => "memory";

    public Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _items.Clear();
        return Task.CompletedTask;
    }

    public void Add(TextChunk chunk, ReadOnlyMemory<float> vector) =>
        _items.Add((chunk, vector.ToArray()));

    public Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(i => i.Chunk.Id == chunk.Id);
        Add(chunk, vector);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ScoredChunk> Search(ReadOnlyMemory<float> query, int topK = 3)
    {
        var q = query.ToArray();
        return _items
            .Select(item => new ScoredChunk(item.Chunk, Cosine.Similarity(q, item.Vector)))
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();
    }

    public Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default) =>
        Task.FromResult(Search(query, topK));
}
