# C# AI practice (job track)

Do the labs in order. Each row is one sitting: read the **Learn** column, run the **Code**, then say the **Rehearse** line out loud like an interview.

You already have **01** and **02**. This branch adds **00** and **03–10**.

## What maps to what

| Word people say | What you actually build | This repo |
| --- | --- | --- |
| Generative AI | Prompt in, text out | 01, 03 |
| Tool calling / function calling | Model picks a C# method, you run it, send the result back | 05 |
| Agentic AI / AI agent | A loop: think → maybe call tools → observe → stop | 07 |
| RAG | Search your docs, then generate using only those snippets | 06 |
| Semantic Kernel | Microsoft library that wraps chat + plugins | 08 (optional) |
| MCP | A protocol so the same tools work in Cursor, your app, or anyone's client | 09 + 10 |
| Embeddings | Vectors for “texts that mean similar things” | 06 |

## Models on your laptop

| You have / need | Role | Command |
| --- | --- | --- |
| Ollama | Local server at `http://localhost:11434` | already installed |
| `llama3.2` (not “ollama3.2”) | Chat / tools / agent | `ollama list` — pull with `ollama pull llama3.2` |
| `nomic-embed-text` | Embeddings for RAG | `ollama pull nomic-embed-text` (274MB). Chat models do **not** embed. |
| HashBag toy embedder | Practice RAG with no extra download | default in lab 06 |
| Cursor | IDE + coding agent (this chat) | **You cannot call Cursor as a model from C#.** C# calls Ollama / OpenAI / GitHub Models / Azure. Use Cursor to write and debug the C#. |

`--provider` on every new lab: `ollama` (default) → `openai` → `github` → `azure` → `fake` (no model, for rehearsal).

## Order

| # | Lab | Learn (one idea) | Code | Rehearse (say this) | SK? | MCP? |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | `00-check` | Ollama is a server; models are files it loads | `dotnet run --project 00-check/CheckOllama` | “I list tags on :11434 and check `llama3.2` vs `nomic-embed-text`.” | no | no |
| 1 | `01-local-chat` | System / user / assistant messages; history | already in repo | “The model has no memory; we resend the list.” | no | no |
| 2 | `02-cloud-switch` | `IChatClient` so the app is not glued to one vendor | already in repo | “Same loop, different client: Ollama or GitHub Models.” | no | no |
| 3 | `03-prompts` | System prompt, few-shot, JSON, “say I don’t know” | `dotnet run --project 03-prompts/Prompts` | “Prompt engineering is instructions + examples, not a framework.” | no | no |
| 4 | `05-tools` | Tool schema → model call → execute → second model call | `dotnet run --project 05-tools/ToolCalling` | “Tool calling is the model choosing a function, not the function calling the model.” | no | no |
| 5 | `06-rag` | Chunk → embed → cosine top-k → grounded answer | `dotnet run --project 06-rag/LocalRag` then `--real-embeddings` | “RAG retrieves text first. Fine-tuning is the wrong default.” | no | no |
| 6 | `07-agent` | Agent = tool loop with a goal, not a chatbot | `dotnet run --project 07-agent/SimpleAgent` | “Chat is one reply. An agent keeps going until the goal is done.” | no | no |
| 7 | `08-semantic-kernel` | Kernel + plugins = same tools, SK API | `dotnet run --project 08-semantic-kernel/SkPlugins` | “SK is optional orchestration. I can do the same with `IChatClient`.” | **yes** | no |
| 8 | `09-mcp-server` | MCP server exposes tools over stdio | started by lab 10 | “The server does not talk to the LLM. It only runs tools.” | no | **server** |
| 9 | `10-mcp-client` | Client lists/calls tools; optional agent uses them | `dotnet run --project 10-mcp-client/DeskMcpClient` then `--agent` | “MCP is USB-C for tools. Cursor is one host; my C# app is another.” | no | **client** |
| 10 | Same labs, cloud | Swap provider, do not rewrite the lab | `--provider openai` then `--provider azure` | “Azure OpenAI is the same `IChatClient` with an endpoint, key, and **deployment name**.” | both | both |

Classic ML (`04-linearRegression`) is optional and is **not** GenAI.

## Run on your Dell (Windows)

```bat
dotnet test tests/AiLabs.Tests/AiLabs.Tests.csproj
dotnet run --project 00-check/CheckOllama
dotnet run --project 03-prompts/Prompts
dotnet run --project 05-tools/ToolCalling
dotnet run --project 05-tools/ToolCalling -- --provider fake
dotnet run --project 06-rag/LocalRag
dotnet run --project 06-rag/LocalRag -- --real-embeddings
dotnet run --project 07-agent/SimpleAgent -- "How many leave days and what time is it?"
dotnet run --project 08-semantic-kernel/SkPlugins
dotnet run --project 10-mcp-client/DeskMcpClient
dotnet run --project 10-mcp-client/DeskMcpClient -- --agent "What is the laptop refresh policy?"
```

Secrets (never commit them):

```bat
set OPENAI_API_KEY=...
set AZURE_OPENAI_ENDPOINT=https://YOUR.openai.azure.com/
set AZURE_OPENAI_API_KEY=...
set AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
set GITHUB_TOKEN=...
```

## Do not study these old files as truth

`SimpleGenAIExUseCSharpOllama3.2.md` and `RAG ExampleUseCSharp.md` were wrong (fake `OLLA` package, RAG is **Retrieval** Augmented Generation). Use the labs above instead.
