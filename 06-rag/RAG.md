# RAG in C# (only this topic)

RAG = **Retrieval Augmented Generation**. Five steps. Then you send **one prompt** to `llama3.2` via Ollama.

```
document  →  chunks  →  embeddings (vectors)  →  vector store
question  →  embed   →  nearest chunks        →  prompt + llama3.2
```

**Not required:** function calling, Semantic Kernel, MCP, MCP server/client, agents. Those are later labs.

## Exact C# steps

| Step | What | This repo |
| --- | --- | --- |
| 1. Chunk | Split handbook into paragraphs | `TextChunker.Split` |
| 2. Embed | Each chunk → `float[]` | `--embed hashbag` or `ollama` |
| 3. Store | Save (text, vector) | `--store memory\|sqlite\|qdrant\|postgres\|sqlserver` |
| 4. Retrieve | Embed the question, take top-k cosine / native NN | `RagPipeline.RetrieveAsync` |
| 5. Generate | Put hits in `PromptCatalog.Rag`, call `IChatClient` | Ollama `llama3.2` |

That last prompt is just text. Offline LLM is correct.

## Embeddings

| Kind | How famous | Command | Notes |
| --- | --- | --- | --- |
| Simplest | toy | `--embed hashbag` (default) | Word overlap. No download. Not for production. |
| Local / job-useful | common | `--embed ollama` after `ollama pull nomic-embed-text` | Real vectors, still offline. `llama3.2` cannot embed. |
| Cloud / famous | very common | `--embed openai` + `OPENAI_API_KEY` | `text-embedding-3-small`. ChatGPT Plus is not this key. |

## Vector stores

| Store | Famous for | Run now? | Command |
| --- | --- | --- | --- |
| In-memory `List<(text, float[])>` | learning | yes | `--store memory` |
| SQLite file | simplest DB | yes | `--store sqlite` |
| **Qdrant** (you meant this, not “qtran”) | dedicated vector DB | Docker | `--store qdrant` |
| **Postgres + pgvector** | SQL shops | Docker | `--store postgres --connection "..."` |
| SQL Server | Microsoft shops | if you have it | `--store sqlserver --connection "..."` |
| Azure AI Search, Redis, Pinecone | cloud products | later | same interface, different client |

Qdrant: `docker run -p 6333:6333 qdrant/qdrant`

Postgres: `docker run -e POSTGRES_PASSWORD=rag -p 5432:5432 pgvector/pgvector:pg16`  
then `--connection "Host=localhost;Username=postgres;Password=rag;Database=postgres"`

## Commands (offline `llama3.2`)

```bat
dotnet run --project 06-rag/LocalRag
dotnet run --project 06-rag/LocalRag -- --store sqlite
dotnet run --project 06-rag/LocalRag -- --embed ollama --store sqlite
dotnet run --project 06-rag/LocalRag -- --store qdrant
```

## What you do next (not now)

| Later | Why after RAG |
| --- | --- |
| Function calling | Model picks a C# method. Different from retrieve-then-prompt. |
| `IChatClient` vs Semantic Kernel | Same RAG either way. SK is optional wrapping. |
| MCP server/client | How tools are exposed to many hosts. Not how RAG retrieves. |
| Agents | Loop that may **call** RAG or tools. RAG can sit inside an agent later. |
| Guardrails | After the happy path works. |
