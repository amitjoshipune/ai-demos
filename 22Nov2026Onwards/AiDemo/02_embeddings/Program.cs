using Microsoft.Extensions.AI;
using OllamaSharp;
namespace _02_embeddings
{
    internal class Program 
    {
        private const string OllamaUrl = "http://127.0.0.1:11434";
        private const string EmbedModel = "nomic-embed-text:latest";
        private static async Task Main(string[] args)
        {
            IEmbeddingGenerator<string, Embedding<float>> embedder = new OllamaApiClient(new Uri(OllamaUrl), EmbedModel);

            string text = "The capital of India is New Delhi.";

            Console.WriteLine("URL:   " + OllamaUrl);
            Console.WriteLine("Model: " + EmbedModel);
            Console.WriteLine("Text:  " + text);
            Console.WriteLine("=============================================");

            Embedding<float> embedding = await embedder.GenerateAsync(text);

            float[] values = embedding.Vector.ToArray();

            Console.WriteLine("Length: " + values.Length);
            Console.WriteLine("(nomic-embed-text must be 768. If this is 3072, you used llama3.2 by mistake.)");
            Console.WriteLine();
            Console.WriteLine("All 768 numbers:");
            Console.WriteLine("=============================================");

            for(int i = 0; i < values.Length; i++)
            {
                Console.Write(values[i].ToString("0.0000"));
                if (i < values.Length - 1)
                {
                    Console.Write(", ");
                }

                if ((i + 1) % 8 == 0)
                {
                    Console.WriteLine();
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("=============================================");

            float[] similar = (await embedder.GenerateAsync("New Delhi is the capital of India.")).Vector.ToArray();
            float[] same = (await embedder.GenerateAsync("The capital of India is New Delhi.")).Vector.ToArray();
            float[] other = (await embedder.GenerateAsync("How to create Tea")).Vector.ToArray();

            Console.WriteLine("cosine(text, similar sentence) = " + CosineSimilarity(values, similar).ToString("0.0000"));
            Console.WriteLine("cosine(text, same sentence)    = " + CosineSimilarity(values, same).ToString("0.0000"));
            Console.WriteLine("cosine(text, Tea)             = " + CosineSimilarity(values, other).ToString("0.0000"));
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("Same/similar text → high cosine (near 1). Tea → lower. That is retrieval.");
        }

        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
            {
                throw new ArgumentException("Vectors must be of the same length.");
            }
            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }
            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);
            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 0; // Avoid division by zero
            }
            return dotProduct / (magnitudeA * magnitudeB);
        }
    }// End of class Program
}// End of namespace _02_embeddings
