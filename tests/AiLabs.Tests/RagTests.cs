using AiLabs.Chat;
using AiLabs.Rag;
using Microsoft.Extensions.AI;

namespace AiLabs.Tests;

public class RagTests
{
    [Fact]
    public void Cosine_of_identical_vectors_is_one()
    {
        float[] v = [1, 2, 3];
        Assert.Equal(1f, Cosine.Similarity(v, v), 5);
    }

    [Fact]
    public void Chunker_splits_handbook_into_sections()
    {
        var chunks = TextChunker.Split(CompanyHandbook.Text);
        Assert.True(chunks.Count >= 3);
        Assert.Contains(chunks, c => c.Text.Contains("20 days"));
    }

    [Fact]
    public async Task HashBag_retrieves_leave_chunk()
    {
        var rag = new RagPipeline(new HashBagEmbeddingGenerator());
        await rag.IndexAsync(CompanyHandbook.Text);
        var hits = await rag.RetrieveAsync("how many annual leave days do employees get?");
        Assert.Contains(hits, h => h.Chunk.Text.Contains("20 days"));
        Assert.True(hits[0].Score > 0);
    }

    [Fact]
    public async Task HashBag_ranks_related_text_above_unrelated()
    {
        IEmbeddingGenerator<string, Embedding<float>> embedder = new HashBagEmbeddingGenerator();
        var leave = await embedder.GenerateVectorAsync("annual leave days policy");
        var leaveDoc = await embedder.GenerateVectorAsync("employees get 20 days of annual leave");
        var cake = await embedder.GenerateVectorAsync("chocolate cake recipe sugar flour");
        var related = Cosine.Similarity(leave.Span, leaveDoc.Span);
        var unrelated = Cosine.Similarity(leave.Span, cake.Span);
        Assert.True(related > unrelated);
    }

    [Fact]
    public async Task Rag_pipeline_answers_from_context_with_fake_model()
    {
        var rag = new RagPipeline(new HashBagEmbeddingGenerator());
        await rag.IndexAsync(CompanyHandbook.Text);
        IChatClient chat = new DemoChatClient();
        var (answer, hits) = await rag.AskAsync(chat, "How many annual leave days?");
        Assert.NotEmpty(hits);
        Assert.Contains("20", answer);
    }

    [Fact]
    public async Task Sqlite_store_round_trips_the_handbook()
    {
        var path = Path.Combine(Path.GetTempPath(), "rag-test-" + Guid.NewGuid().ToString("n") + ".db");
        try
        {
            using var store = new SqliteVectorStore(path);
            var rag = new RagPipeline(new HashBagEmbeddingGenerator(), store);
            await rag.IndexAsync(CompanyHandbook.Text);
            var hits = await rag.RetrieveAsync("how many annual leave days do employees get?");
            Assert.Contains(hits, h => h.Chunk.Text.Contains("20 days"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Store_factory_rejects_unknown_names()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => VectorStoreFactory.Create("pinecone", null));
        Assert.Contains("sqlite", ex.Message);
    }

    [Fact]
    public async Task Rag_pipeline_refuses_facts_not_in_the_handbook()
    {
        var rag = new RagPipeline(new HashBagEmbeddingGenerator());
        await rag.IndexAsync(CompanyHandbook.Text);
        IChatClient chat = new DemoChatClient();
        var (answer, _) = await rag.AskAsync(chat, "What is the CEO salary?");
        Assert.Contains("I don't know", answer, StringComparison.OrdinalIgnoreCase);
    }
}
