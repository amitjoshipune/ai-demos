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

Postgres: set `RAG_CONNECTION` to your Docker string (do not commit the password).
The lab tries `CREATE EXTENSION vector`. If that image is plain Postgres (no pgvector), it stores BYTEA and scores cosine in C#.

SQL Server 2019 (`LocalDell`): `--store sqlserver` with `RAG_CONNECTION`. Vectors are VARBINARY + cosine in C#. You do not need SQL Server 2025 VECTOR type.

## Commands (offline `llama3.2` + `nomic-embed-text`)

Same idea as the DEV.to post (Ollama + embeddings + a DB). That post uses Gemma 3, mxbai-embed-large (1024-d), Semantic Kernel, and Pokémon. This lab uses **llama3.2**, **nomic-embed-text (768-d)**, **IChatClient**, and the Contoso handbook. SK is not required.

```bat
REM prove Ollama (you already did):
curl http://localhost:11434/api/tags

REM 1) no database
dotnet run --project 06-rag/LocalRag -- --embed ollama

REM 2) Docker Postgres (plain Postgres is OK; pgvector is a bonus)
set RAG_CONNECTION=Host=localhost;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD
dotnet run --project 06-rag/LocalRag -- --embed ollama --store postgres

REM 3) worst case: local SQL Server 2019
set RAG_CONNECTION=Server=YOUR_PC\YOUR_INSTANCE;Database=YOUR_DB;Trusted_Connection=True;TrustServerCertificate=True;
dotnet run --project 06-rag/LocalRag -- --embed ollama --store sqlserver
```

If Ollama is bound on another NIC, add `--ollama-url http://127.0.0.1:11434`.

## What you do next (not now)

| Later | Why after RAG |
| --- | --- |
| Function calling | Model picks a C# method. Different from retrieve-then-prompt. |
| `IChatClient` vs Semantic Kernel | Same RAG either way. SK is optional wrapping. |
| MCP server/client | How tools are exposed to many hosts. Not how RAG retrieves. |
| Agents | Loop that may **call** RAG or tools. RAG can sit inside an agent later. |
| Guardrails | After the happy path works. |
