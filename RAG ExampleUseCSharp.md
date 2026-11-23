This note was incorrect. RAG means **Retrieval Augmented Generation**, not "Reward Augmented Guidance", and you do not train Ollama with ML.NET.

Use lab `06-rag/LocalRag`:

1. Split the handbook into chunks.
2. Embed each chunk (`nomic-embed-text`, or the HashBag toy embedder).
3. Embed the question, take top cosine matches.
4. Ask `llama3.2` to answer using only that context.

See `PRACTICE.md`.
