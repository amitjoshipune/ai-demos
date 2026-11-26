using Microsoft.Data.SqlClient;

namespace AiLabs.Rag;

/// <summary>
/// SQL Server path that works on ordinary SQL Server: store the vector as VARBINARY, cosine in C#.
/// Famous newer way (Azure SQL / SQL Server 2025) is VECTOR type + VECTOR_DISTANCE — see comment in SearchAsync.
/// Set RAG_CONNECTION to a SQL Server connection string.
/// </summary>
public sealed class SqlServerVectorStore : IVectorStore, IDisposable
{
    private readonly string _connectionString;

    public SqlServerVectorStore(string connectionString) => _connectionString = connectionString;

    public string Name => "sqlserver";

    public async Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF OBJECT_ID('dbo.RagChunks', 'U') IS NULL
            CREATE TABLE dbo.RagChunks (
                Id nvarchar(64) NOT NULL PRIMARY KEY,
                Body nvarchar(max) NOT NULL,
                Embedding varbinary(max) NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF OBJECT_ID('dbo.RagChunks', 'U') IS NOT NULL
                DELETE FROM dbo.RagChunks;
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        var bytes = VectorCodec.ToBytes(vector.Span);
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            MERGE dbo.RagChunks AS t
            USING (SELECT @id AS Id) AS s
            ON t.Id = s.Id
            WHEN MATCHED THEN UPDATE SET Body = @body, Embedding = @vec
            WHEN NOT MATCHED THEN INSERT (Id, Body, Embedding) VALUES (@id, @body, @vec);
            """;
        cmd.Parameters.AddWithValue("@id", chunk.Id);
        cmd.Parameters.AddWithValue("@body", chunk.Text);
        cmd.Parameters.AddWithValue("@vec", bytes);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default)
    {
        // Native (when you have VECTOR columns):
        // SELECT TOP (@k) Id, Body, VECTOR_DISTANCE('cosine', Embedding, @q) AS score
        // FROM dbo.RagChunks ORDER BY score;
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Body, Embedding FROM dbo.RagChunks;";
        var hits = new List<ScoredChunk>();
        var q = query.ToArray();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var vector = VectorCodec.FromBytes((byte[])reader["Embedding"]);
            hits.Add(new ScoredChunk(
                new TextChunk(reader.GetString(0), reader.GetString(1)),
                Cosine.Similarity(q, vector)));
        }

        return hits.OrderByDescending(h => h.Score).Take(topK).ToList();
    }

    public void Dispose()
    {
    }
}
