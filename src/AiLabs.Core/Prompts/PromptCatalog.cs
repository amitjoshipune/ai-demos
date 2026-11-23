namespace AiLabs.Prompts;

/// <summary>Four prompt patterns you should be able to explain in an interview.</summary>
public static class PromptCatalog
{
    public const string Chat = "chat";
    public const string Json = "json";
    public const string FewShot = "fewshot";
    public const string Grounded = "grounded";

    public static string SystemFor(string style) => style.ToLowerInvariant() switch
    {
        Json => """
            Return ONLY valid JSON. No markdown. Shape:
            {"intent":"question|request|other","topic":"short phrase","answer":"one sentence"}
            """,
        FewShot => """
            Classify the user's message as Billing, Leave, Laptop, or Other.
            Examples:
            User: invoice is wrong → Billing
            User: I need PTO next week → Leave
            User: my notebook is 4 years old → Laptop
            Answer with the label only.
            """,
        Grounded => """
            Answer using only facts the user provided. If the answer is not there, say exactly: I don't know.
            Keep it to 2 sentences.
            """,
        _ => """
            You are a concise workplace assistant. Answer in at most 4 lines.
            If you are not sure, say you are not sure.
            """
    };

    public static string Rag(string question, IEnumerable<string> chunks)
    {
        var context = string.Join("\n\n---\n\n", chunks);
        return $"""
            Use ONLY the context. If the answer is not in the context, say: I don't know.
            Quote the relevant sentence. Do not invent policy.

            Context:
            {context}

            Question: {question}
            """;
    }

    public static string Agent => """
        You are a desk agent for Contoso. Use tools when they help. When you have enough
        information, give a short final answer. Do not ask the user to run tools themselves.
        """;
}
