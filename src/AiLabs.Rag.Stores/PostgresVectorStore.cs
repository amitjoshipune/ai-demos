using Npgsql;

namespace AiLabs.Rag;

/// <summary>
/// Famous SQL path: PostgreSQL + pgvector. Native nearest-neighbour: embedding &lt;=&gt; query.
/// Example: docker run -e POSTGRES_PASSWORD=rag -p 5432:5432 pgvector/pgvector:pg16
/// Connection: Host=localhost;Username=postgres;Password=rag;Database=postgres
/// </summary>
public sealed class PostgresVectorStore : IVectorStore, IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;
    private int _dimensions;

    public PostgresVectorStore(string connectionString)
    {
        _source = NpgsqlDataSource.Create(connectionString);
    }

    public string Name => "postgres+pgvector";

    public async Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        _dimensions = dimensions;
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using (var ext = conn.CreateCommand())
        {
            ext.CommandText = "CREATE EXTENSION IF NOT EXISTS vector;";
            await ext.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var table = conn.CreateCommand();
        table.CommandText = $"""
            CREATE TABLE IF NOT EXISTS rag_chunks (
                id TEXT PRIMARY KEY,
                body TEXT NOT NULL,
                embedding vector({dimensions}) NOT NULL
            );
            """;
        await table.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rag_chunks;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (_dimensions > 0)
            await EnsureAsync(_dimensions, cancellationToken);
    }

    public async Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        var emb = VectorCodec.ToBracketList(vector.Span);
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO rag_chunks (id, body, embedding)
            VALUES (@id, @body, CAST(@emb AS vector))
            ON CONFLICT (id) DO UPDATE SET body = EXCLUDED.body, embedding = EXCLUDED.embedding;
            """;
        cmd.Parameters.AddWithValue("id", chunk.Id);
        cmd.Parameters.AddWithValue("body", chunk.Text);
        cmd.Parameters.AddWithValue("emb", emb);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default)
    {
        var qText = VectorCodec.ToBracketList(query.Span);
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, body, 1 - (embedding <=> CAST(@q AS vector)) AS score
            FROM rag_chunks
            ORDER BY embedding <=> CAST(@q AS vector)
            LIMIT @k;
            """;
        cmd.Parameters.AddWithValue("q", qText);
        cmd.Parameters.AddWithValue("k", topK);
        var hits = new List<ScoredChunk>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hits.Add(new ScoredChunk(
                new TextChunk(reader.GetString(0), reader.GetString(1)),
                reader.GetFloat(2)));
        }

        return hits;
    }

    public async ValueTask DisposeAsync() => await _source.DisposeAsync();
}
