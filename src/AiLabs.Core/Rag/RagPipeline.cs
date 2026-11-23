using AiLabs.Prompts;
using Microsoft.Extensions.AI;

namespace AiLabs.Rag;

public sealed class RagPipeline
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly InMemoryVectorStore _store = new();
    private readonly List<TextChunk> _chunks = [];

    public RagPipeline(IEmbeddingGenerator<string, Embedding<float>> embedder) => _embedder = embedder;

    public IReadOnlyList<TextChunk> Chunks => _chunks;

    public async Task IndexAsync(string document, CancellationToken cancellationToken = default)
    {
        _chunks.Clear();
        var chunks = TextChunker.Split(document);
        var embeddings = await _embedder.GenerateAsync(chunks.Select(c => c.Text), cancellationToken: cancellationToken);
        for (var i = 0; i < chunks.Count; i++)
        {
            _chunks.Add(chunks[i]);
            _store.Add(chunks[i], embeddings[i].Vector);
        }
    }

    public async Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(string question, int topK = 3, CancellationToken cancellationToken = default)
    {
        var q = await _embedder.GenerateVectorAsync(question, cancellationToken: cancellationToken);
        return _store.Search(q, topK);
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
