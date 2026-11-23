using Microsoft.Data.Sqlite;

namespace AiLabs.Rag;

/// <summary>
/// Simplest "real database" RAG store: a file on disk, cosine in C#.
/// No SQL Server, no Docker. Good enough for a handbook-sized corpus.
/// </summary>
public sealed class SqliteVectorStore : IVectorStore, IDisposable
{
    private readonly string _path;
    private readonly SqliteConnection _db;

    public SqliteVectorStore(string path)
    {
        _path = path;
        _db = new SqliteConnection($"Data Source={path}");
        _db.Open();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS chunks (
                id TEXT PRIMARY KEY,
                body TEXT NOT NULL,
                embedding BLOB NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public string Name => $"sqlite:{_path}";

    public Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM chunks;";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO chunks (id, body, embedding) VALUES ($id, $body, $vec)
            ON CONFLICT(id) DO UPDATE SET body = excluded.body, embedding = excluded.embedding;
            """;
        cmd.Parameters.AddWithValue("$id", chunk.Id);
        cmd.Parameters.AddWithValue("$body", chunk.Text);
        cmd.Parameters.AddWithValue("$vec", VectorCodec.ToBytes(vector.Span));
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id, body, embedding FROM chunks;";
        using var reader = cmd.ExecuteReader();
        var hits = new List<ScoredChunk>();
        var q = query.Span;
        while (reader.Read())
        {
            var vector = VectorCodec.FromBytes((byte[])reader["embedding"]);
            hits.Add(new ScoredChunk(
                new TextChunk(reader.GetString(0), reader.GetString(1)),
                Cosine.Similarity(q, vector)));
        }

        IReadOnlyList<ScoredChunk> top = hits.OrderByDescending(h => h.Score).Take(topK).ToList();
        return Task.FromResult(top);
    }

    public void Dispose() => _db.Dispose();
}
