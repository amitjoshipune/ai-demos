using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace _03_localRagMemory
{
    internal class Program
    {
        private const string OllamaUrl = "http://127.0.0.1:11434";
        private const string ChatModel = "llama3.2:latest";
        private const string EmbedModel = "nomic-embed-text:latest";
        private const int TopK = 3;
        public static async Task Main(string[] args)
        {
            IChatClient chat = new OllamaApiClient(new Uri(OllamaUrl), ChatModel);
            IEmbeddingGenerator<string, Embedding<float>> embedder = new OllamaApiClient(new Uri(OllamaUrl), EmbedModel);


            string handbook =
                "Contoso PTO: full-time employees get 18 vacation days per year. Unused days do not roll over.\n\n" +
                "Contoso work hours: office hours are 9:00 to 17:30 Monday to Friday. Remote Friday is allowed after 6 months.\n\n" +
                "Contoso laptops: every engineer gets a Dell laptop. Lost devices must be reported to IT within 4 hours.\n\n" +
                "Contoso expenses: meals under 25 USD need no receipt. Flights need manager approval before booking.";

            string[] parts = handbook.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<Chunk> chunks = new List<Chunk>();
            Console.WriteLine("Step 1-3: chunk, embed, store in a List (RAM).");
            Console.WriteLine();


            for (int i = 0; i < parts.Length; i++)
            {
                string text = parts[i].Trim();
                Embedding<float> embedding = await embedder.GenerateAsync(text);
                float[] vector = embedding.Vector.ToArray();

                Chunk chunk = new Chunk();
                chunk.Id = "c" + (i + 1);
                chunk.Text = text;
                chunk.Vector = vector;
                chunks.Add(chunk);

                Console.WriteLine(chunk.Id + "  dim=" + vector.Length + "  " + Preview(text, 70));
            }//End of for loop over parts

            Console.WriteLine();
            Console.WriteLine("Step 4-5: embed question, cosine, stuff top " + TopK + " into a user message, call llama3.2.");
            Console.WriteLine("Empty line = quit.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("You: ");
                string? question = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(question))
                {
                    break;
                }
                float[] query = (await embedder.GenerateAsync(question)).Vector.ToArray();

                List<ScoredChunk> ranked = new List<ScoredChunk>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    ScoredChunk row = new ScoredChunk();
                    row.chunk = chunks[i];
                    row.Score = Cosine(query, chunks[i].Vector);
                    ranked.Add(row);
                }
                ranked.Sort((a, b) => b.Score.CompareTo(a.Score));

                Console.WriteLine();
                Console.WriteLine("--- retrieval (higher cosine = closer) ---");

                int take = Math.Min(TopK, ranked.Count);
                for (int i = 0; i < ranked.Count; i++)
                {
                    string mark = i < take ? "Keep" : "skip";
                    Console.WriteLine(
                       "  " + mark + "  " + ranked[i].chunk.Id +
                       "  cosine=" + ranked[i].Score.ToString("0.0000") +
                       "  " + Preview(ranked[i].chunk.Text, 60));
                }// End of for loop over ranked


                string context = "";
                for (int i = 0; i < take; i++)
                {
                    if (i > 0)
                    {
                        context += "\n\n";
                    }
                    context += ranked[i].chunk.Text;
                }// End of for loop to build context

                List<ChatMessage> messages = new List<ChatMessage>();
                messages.Add(new ChatMessage(ChatRole.System, "Answer only from CONTEXT. If the answer is not in CONTEXT, say you do not know. 2-3 short sentences."));
                messages.Add(new ChatMessage(ChatRole.User, "CONTEXT:\n" + context + "\n\nQUESTION:\n" + question));

                Dump("messages sent to llama3.2", messages);

                try
                {
                    ChatResponse response = await chat.GetResponseAsync(messages);
                    string text = response.Text ?? "";
                    Console.WriteLine("AI [" + ChatRole.Assistant + "]: " + text);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine("Is Ollama running?  ollama list");
                }

            }// End of while loop for user input


        }// End of Main

        private static double Cosine(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                throw new InvalidOperationException("Vectors must be the same length.");
            }

            double dot = 0;
            double normA = 0;
            double normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private static void Dump(string title, List<ChatMessage> messages)
        {
            Console.WriteLine("--- " + title + " ---");
            for (int i = 0; i < messages.Count; i++)
            {
                Console.WriteLine("  " + i + ": [" + messages[i].Role + "] " + Preview(messages[i].Text ?? "", 90));
            }

            Console.WriteLine("----------------------------");
        }
        private static string Preview(string text, int max)
        {
            string oneLine = text.Replace("\n", " ");
            if (oneLine.Length <= max)
            {
                return oneLine;
            }

            return oneLine.Substring(0, max) + "...";

        }
    }// End of class Program

    internal class Chunk
    {
        public string Id = "";
        public string Text = "";
        public float[] Vector = Array.Empty<float>();
    }//end of class Chunk

    internal class ScoredChunk
    {
        public Chunk chunk = new Chunk();
        public double Score;
    }
}// End of namespace _03_localRagMemory