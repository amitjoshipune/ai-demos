
using Microsoft.Extensions.AI;
using OllamaSharp;

using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

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
            IChatClient chat = new OllamaApiClient(new Uri(OllamaUrl), ChatModel);
            IEmbeddingGenerator<string, Embedding<float>> embedder = new OllamaApiClient(new Uri(OllamaUrl), EmbedModel);

            using HttpClient qdrant = new HttpClient();
            qdrant.BaseAddress = new Uri(QdrantUrl);
            qdrant.Timeout = TimeSpan.FromSeconds(60);

            string handbook =
                "Contoso PTO: full-time employees get 18 vacation days per year. Unused days do not roll over.\n\n" +
                "Contoso work hours: office hours are 9:00 to 17:30 Monday to Friday. Remote Friday is allowed after 6 months.\n\n" +
                "Contoso laptops: every engineer gets a Dell laptop. Lost devices must be reported to IT within 4 hours.\n\n" +
                "Contoso expenses: meals under 25 USD need no receipt. Flights need manager approval before booking.";

            await ResetCollection(qdrant, 768);

            string[] parts = handbook.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Index: embed chunks, upsert into Qdrant collection '" + Collection + "'.");
            Console.WriteLine();

            for (int i = 0; i < parts.Length; i++)
            {
                string text = parts[i].Trim();
                float[] vector = (await embedder.GenerateAsync(text)).Vector.ToArray();
                int pointId = i + 1;

                /*
                Console.WriteLine("Upserting point " + pointId + "  dim=" + vector.Length + "  " + text);
                Console.WriteLine("[");

                Console.WriteLine(string.Join(",", vector.Select(v => v.ToString("F2"))));
                Console.WriteLine("]");
                */

                object body = new 
                { 
                    points = new[]
                    {
                        new
                        {
                            id = pointId,
                            vector = vector,
                            payload = new { chunkId = "c" + pointId, text = text }
                        }
                    }
                };

                using HttpResponseMessage upsert = await qdrant.PutAsJsonAsync(
                    "collections/" + Collection + "/points?wait=true",
                    body
                    );

                await EnsureOk(upsert,"upsert point " + pointId);
                Console.WriteLine("  stored c" + pointId + "  dim=" + vector.Length + "  " + Preview(text, 70));
            }// End of for loop over parts

            Console.WriteLine();
            Console.WriteLine("Open http://127.0.0.1:6333/dashboard → Collections → handbook (4 points).");
            Console.WriteLine("Ask a question. Empty line = quit.");
            Console.WriteLine();

        }// End of Main

        private static string Preview(string text, int max)
        {
            string oneLine = text.Replace("\n", " ");
            return oneLine.Length <= max ? oneLine : oneLine.Substring(0, max) + "...";
        }

        private static async Task ResetCollection(HttpClient qdrant, int dimensions)
        {
            using HttpResponseMessage delete = await qdrant.DeleteAsync("collections/" + Collection);
            if (!delete.IsSuccessStatusCode && (int)delete.StatusCode != 404)
            {
                // throw new InvalidOperationException("Failed to reset collection");
                await EnsureOk(delete, "delete collection");
            }

            object create = new
            {
                vectors = new { size = dimensions, distance = "Cosine" }
            };

            using HttpResponseMessage put = await qdrant.PutAsJsonAsync("collections/" + Collection, create);
            await EnsureOk(put, "create collection");

        }// End of ResetCollection

        private static async Task<string> EnsureOk(HttpResponseMessage response, string what)
        {
            string json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(what + " failed: " + (int)response.StatusCode + " " + json);
            }

            return json;
        }// End of EnsureOk
    }// End of class Program
}// End of namespace _04_qdrantRag