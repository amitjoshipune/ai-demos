using Microsoft.Extensions.AI;
using OllamaSharp;

namespace _01_localLLMChat
{
    internal class Program
    {
        private const string OllamaUrl = "http://192.168.1.6:11434";// "http://127.0.0.1:11434";
        private const string OllamaModel = "llama3.2:latest";

        private static async Task Main(string[] args)
        {
            IChatClient client = new OllamaApiClient(new Uri(OllamaUrl), OllamaModel);

            List<ChatMessage> history = new List<ChatMessage>();
            history.Add(new ChatMessage(
                ChatRole.System,
                "You are a helpful assistant. Answer in 2-3 short sentences."));
            history.Add(new ChatMessage(
                ChatRole.User,
                "What is the capital of France?"));
            history.Add(new ChatMessage(
                ChatRole.Assistant,
                "The capital of France is Paris. It is known for its art, culture, and history."));

            Dump("seed history", history);

            Console.WriteLine("Ask a question. Empty line = quit.");
            Console.WriteLine("Try: What country is that in?");
            Console.WriteLine();

            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }

                history.Add(new ChatMessage(ChatRole.User, input));
                Dump("about to send this to llama3.2", history);

                try
                {
                    ChatResponse response = await client.GetResponseAsync(history);
                    string text = response.Text ?? "";

                    Console.WriteLine();
                    Console.WriteLine("AI [" + ChatRole.Assistant + "]: " + text);
                    Console.WriteLine();

                    history.Add(new ChatMessage(ChatRole.Assistant, text));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occurred: " + ex.Message);
                    Console.WriteLine("Is Ollama running?  ollama list");
                    history.RemoveAt(history.Count - 1);
                }
            }
        }

        private static void Dump(string title, List<ChatMessage> history)
        {
            Console.WriteLine("--- " + title + " ---");

            for (int i = 0; i < history.Count; i++)
            {
                ChatMessage m = history[i];
                string preview = (m.Text ?? "").Replace("\n", " ");
                if (preview.Length > 80)
                {
                    preview = preview.Substring(0, 80) + "...";
                }

                Console.WriteLine("  " + i + ": [" + m.Role + "] " + preview);
            }

            Console.WriteLine("----------------------------");
        }
    }
}