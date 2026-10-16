import ollama
messages = [{"role": "system", "content": "You are llama3.2 running locally via Ollama. Answer in at most 3 lines."}]
while True:
    q = input("You: ")
    if not q.strip():
        break
    messages.append({"role": "user", "content": q})
    r = ollama.chat(model="llama3.2", messages=messages)
    print("AI:", r["message"]["content"])
    messages.append(r["message"])