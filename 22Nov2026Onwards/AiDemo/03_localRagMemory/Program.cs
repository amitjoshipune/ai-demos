using Microsoft.Extensions.AI;
using OllamaSharp;

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
            IChatClient chat = new OllamaApiClient(new Uri(OllamaUrl),ChatModel);
            IEmbeddingGenerator<string,Embedding<float>> embedder = new OllamaApiClient(new Uri(OllamaUrl),EmbedModel);


            string handbook =
                "Contoso PTO: full-time employees get 18 vacation days per year. Unused days do not roll over.\n\n" +
                "Contoso work hours: office hours are 9:00 to 17:30 Monday to Friday. Remote Friday is allowed after 6 months.\n\n" +
                "Contoso laptops: every engineer gets a Dell laptop. Lost devices must be reported to IT within 4 hours.\n\n" +
                "Contoso expenses: meals under 25 USD need no receipt. Flights need manager approval before booking.";

            string[] parts = handbook.Split(new[] {"\n\n" },StringSplitOptions.RemoveEmptyEntries);

            List<Chunk> chunks= new List<Chunk>();
            Console.WriteLine("Step 1-3: chunk, embed, store in a List (RAM).");
            Console.WriteLine();


            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                var embedding = await embedder.GenerateAsync(part);
                chunks.Add(new Chunk
                {
                    Id = $"chunk-{i}",
                    Text = part,
                    vector = embedding.Vector.ToArray()
                });
            }

        }// End of Main
    }// End of class Program

    internal class Chunk
    {
        public string Id = "";
        public string Text = "";
        public float[] vector = Array.Empty<float>();
    }//end of class Chunk

}// End of namespace _03_localRagMemory