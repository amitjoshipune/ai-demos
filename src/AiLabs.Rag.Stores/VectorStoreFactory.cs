namespace AiLabs.Rag;

public static class VectorStoreFactory
{
    public static IVectorStore Create(string store, string? connection)
    {
        var name = store.Trim().ToLowerInvariant();
        return name switch
        {
            "memory" or "inmemory" => new InMemoryVectorStore(),
            "sqlite" => new SqliteVectorStore(connection ?? Path.Combine(Directory.GetCurrentDirectory(), "rag-handbook.db")),
            "qdrant" => new QdrantVectorStore(connection ?? Environment.GetEnvironmentVariable("QDRANT_URL") ?? "http://localhost:6333"),
            "postgres" or "pg" or "pgvector" => new PostgresVectorStore(
                Require(connection ?? Environment.GetEnvironmentVariable("RAG_CONNECTION"),
                    "Postgres: set --connection or RAG_CONNECTION. Example: Host=localhost;Username=postgres;Password=rag;Database=postgres")),
            "sqlserver" or "mssql" or "sql" => new SqlServerVectorStore(
                Require(connection ?? Environment.GetEnvironmentVariable("RAG_CONNECTION"),
                    "SQL Server: set --connection or RAG_CONNECTION to a SqlClient connection string.")),
            _ => throw new InvalidOperationException(
                $"Unknown --store '{store}'. Use memory | sqlite | qdrant | postgres | sqlserver.")
        };
    }

    private static string Require(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(message) : value;
}
