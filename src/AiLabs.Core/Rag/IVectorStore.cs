namespace AiLabs.Rag;

/// <summary>
/// Where chunks + vectors live. RAG does not care if this is RAM, SQLite, Qdrant, Postgres, or SQL Server.
/// </summary>
public interface IVectorStore
{
    string Name { get; }
    Task EnsureAsync(int dimensions, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(TextChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken = default);
}

public static class VectorCodec
{
    public static byte[] ToBytes(ReadOnlySpan<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        for (var i = 0; i < vector.Length; i++)
            BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), vector[i]);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        var n = bytes.Length / sizeof(float);
        var vector = new float[n];
        for (var i = 0; i < n; i++)
            vector[i] = BitConverter.ToSingle(bytes, i * 4);
        return vector;
    }

    public static string ToBracketList(ReadOnlySpan<float> vector) =>
        "[" + string.Join(",", vector.ToArray().Select(v => v.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))) + "]";

    public static uint StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (var c in s)
                h = (h ^ c) * 16777619;
            return h;
        }
    }
}
