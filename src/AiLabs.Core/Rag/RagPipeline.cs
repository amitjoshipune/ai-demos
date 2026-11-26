using AiLabs.Prompts;
using Microsoft.Extensions.AI;

namespace AiLabs.Rag;

public sealed class RagPipeline
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly IVectorStore _store;
    private readonly List<TextChunk> _chunks = [];

    public RagPipeline(IEmbeddingGenerator<string, Embedding<float>> embedder, IVectorStore? store = null)
    {
        _embedder = embedder;
        _store = store ?? new InMemoryVectorStore();
    }

    public IVectorStore Store => _store;
    public IReadOnlyList<TextChunk> Chunks => _chunks;

    public async Task IndexAsync(string document, CancellationToken cancellationToken = default)
    {
        var chunks = TextChunker.Split(document);
        var embeddings = await _embedder.GenerateAsync(chunks.Select(c => c.Text), cancellationToken: cancellationToken);
        if (chunks.Count == 0)
            throw new InvalidOperationException("Document produced no chunks.");

        await _store.EnsureAsync(embeddings[0].Vector.Length, cancellationToken);
        await _store.ClearAsync(cancellationToken);

        _chunks.Clear();
        for (var i = 0; i < chunks.Count; i++)
        {
            _chunks.Add(chunks[i]);
            await _store.UpsertAsync(chunks[i], embeddings[i].Vector, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(string question, int topK = 3, CancellationToken cancellationToken = default)
    {
        var q = await _embedder.GenerateVectorAsync(question, cancellationToken: cancellationToken);
        return await _store.SearchAsync(q, topK, cancellationToken);
    }

    public async Task<(string Answer, IReadOnlyList<ScoredChunk> Hits)> AskAsync(
        IChatClient chat,
        string question,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        var hits = await RetrieveAsync(question, topK, cancellationToken);
        var prompt = PromptCatalog.Rag(question, hits.Select(h => h.Chunk.Text));
        var response = await chat.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        return (response.Text ?? "", hits);
    }
}
