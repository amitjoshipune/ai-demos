using Npgsql;

namespace AiLabs.Rag;

/// <summary>
/// Postgres RAG store. Prefers pgvector (native &lt;=&gt; search). If the Docker image
/// has no pgvector, or the user is not allowed to CREATE EXTENSION, falls back to BYTEA
/// and cosine in C# — same as SQL Server 2019. nomic-embed-text is 768 dimensions.
/// </summary>
public sealed class PostgresVectorStore : IVectorStore, IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;
    private int _dimensions;
    private bool _usePgvector;

    public PostgresVectorStore(string connectionString)
    {
        _source = NpgsqlDataSource.Create(connectionString);
    }

    public string Name => _usePgvector ? "postgres+pgvector" : "postgres+bytea (no pgvector; cosine in C#)";

    public async Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        _dimensions = dimensions;
        _usePgvector = await TryEnablePgvectorAsync(cancellationToken);
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var table = conn.CreateCommand();
        table.CommandText = _usePgvector
            ? $"""
                CREATE TABLE IF NOT EXISTS rag_chunks (
                    id TEXT PRIMARY KEY,
                    body TEXT NOT NULL,
                    embedding vector({dimensions}) NOT NULL
                );
                """
            : """
                CREATE TABLE IF NOT EXISTS rag_chunks (
                    id TEXT PRIMARY KEY,
                    body TEXT NOT NULL,
                    embedding BYTEA NOT NULL
                );
                """;
        await table.ExecuteNonQueryAsync(cancellationToken);
        if (!_usePgvector)
            Console.WriteLine("Postgres has no pgvector (or CREATE EXTENSION is not allowed). Storing BYTEA and scoring cosine in C#. That is enough for this lab.");
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
        var embText = VectorCodec.ToBracketList(vector.Span);
        var embBytes = VectorCodec.ToBytes(vector.Span);
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        if (_usePgvector)
        {
            cmd.CommandText = """
                INSERT INTO rag_chunks (id, body, embedding)
                VALUES (@id, @body, CAST(@emb AS vector))
                ON CONFLICT (id) DO UPDATE SET body = EXCLUDED.body, embedding = EXCLUDED.embedding;
                """;
            cmd.Parameters.AddWithValue("id", chunk.Id);
            cmd.Parameters.AddWithValue("body", chunk.Text);
            cmd.Parameters.AddWithValue("emb", embText);
        }
        else
        {
            cmd.CommandText = """
                INSERT INTO rag_chunks (id, body, embedding)
                VALUES (@id, @body, @emb)
                ON CONFLICT (id) DO UPDATE SET body = EXCLUDED.body, embedding = EXCLUDED.embedding;
                """;
            cmd.Parameters.AddWithValue("id", chunk.Id);
            cmd.Parameters.AddWithValue("body", chunk.Text);
            cmd.Parameters.AddWithValue("emb", embBytes);
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default)
    {
        if (_usePgvector)
            return await SearchPgvectorAsync(query, topK, cancellationToken);
        return await SearchByteaAsync(query, topK, cancellationToken);
    }

    public async ValueTask DisposeAsync() => await _source.DisposeAsync();

    private async Task<bool> TryEnablePgvectorAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = await _source.OpenConnectionAsync(cancellationToken);
            await using var ext = conn.CreateCommand();
            ext.CommandText = "CREATE EXTENSION IF NOT EXISTS vector;";
            await ext.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException)
        {
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private async Task<IReadOnlyList<ScoredChunk>> SearchPgvectorAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken)
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

    private async Task<IReadOnlyList<ScoredChunk>> SearchByteaAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken)
    {
        var q = query.ToArray();
        await using var conn = await _source.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, body, embedding FROM rag_chunks;";
        var hits = new List<ScoredChunk>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var vector = VectorCodec.FromBytes((byte[])reader["embedding"]);
            hits.Add(new ScoredChunk(
                new TextChunk(reader.GetString(0), reader.GetString(1)),
                Cosine.Similarity(q, vector)));
        }

        return hits.OrderByDescending(h => h.Score).Take(topK).ToList();
    }
}
