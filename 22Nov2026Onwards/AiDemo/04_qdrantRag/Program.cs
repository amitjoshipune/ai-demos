
using Microsoft.Extensions.AI;
using OllamaSharp;

using System.Net.Http.Json;
using System.Text.Json;

namespace _04_qdrantRag
{
    internal class Program
    {
        private const string OllamaUrl = "http://127.0.0.1:11434";
        private const string QdrantUrl = "http://127.0.0.1:6333/";
        private const string ChatModel = "llama3.2:latest";
        private const string EmbedModel = "nomic-embed-text:latest";
        private const string Collection = "handbook";
        private const int TopK = 3;

        private static async Task Main(string[] args)
        {
            IChatClient chat = new OllamaApiClient(new Uri(OllamaUrl),ChatModel);
            IEmbeddingGenerator<string, Embedding<float>> embedder = new OllamaApiClient(new Uri(OllamaUrl),EmbedModel);

            using HttpClient qdrant = new HttpClient();
            qdrant.BaseAddress = new Uri(QdrantUrl);
            qdrant.Timeout = TimeSpan.FromSeconds(60);

            string handbook =
                "Contoso PTO: full-time employees get 18 vacation days per year. Unused days do not roll over.\n\n" +
                "Contoso work hours: office hours are 9:00 to 17:30 Monday to Friday. Remote Friday is allowed after 6 months.\n\n" +
                "Contoso laptops: every engineer gets a Dell laptop. Lost devices must be reported to IT within 4 hours.\n\n" +
                "Contoso expenses: meals under 25 USD need no receipt. Flights need manager approval before booking.";



        }

    }// End of class Program
}// End of namespace _04_qdrantRag