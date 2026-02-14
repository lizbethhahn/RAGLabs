using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.ClientModel;
using System.Numerics.Tensors;

namespace SemanticKernelAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🤖 C# Semantic Kernel Agent Starting...\n");

            // Load configuration from environment variables
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            var githubToken = configuration["GITHUB_TOKEN"];
            
            if (string.IsNullOrEmpty(githubToken))
            {
                Console.WriteLine("❌ Error: GITHUB_TOKEN not found in environment variables.");
                Console.WriteLine("Set it using: $env:GITHUB_TOKEN=\"your-github-token-here\"");
                Console.WriteLine("Or use user secrets: dotnet user-secrets set \"GITHUB_TOKEN\" \"your-github-token-here\"");
                return;
            }

            // Create kernel with GitHub Models chat completion
            var builder = Kernel.CreateBuilder();
            
            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(githubToken), 
                new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") }
            );

            // Add text embedding generation service to the kernel builder
#pragma warning disable CS0618 // Type or member is obsolete
            builder.AddOpenAITextEmbeddingGeneration(
                modelId: "text-embedding-3-small",
                openAIClient: openAIClient
            );
#pragma warning restore CS0618 // Type or member is obsolete

            // Build the kernel
            var kernel = builder.Build();

            // Get the embedding service from the kernel
#pragma warning disable CS0618
            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618


            Console.WriteLine("🔍 Embedding Inspector Lab\n");

            // Define test sentences
            string[] testSentences = {
                "The movie was excellent and entertaining.",
                "The movie was terrible and boring.",
                "I enjoyed watching the film."
            };

            // Output test sentences to the console
            Console.WriteLine("Test Sentences:");
            foreach (var sentence in testSentences)
            {
                Console.WriteLine($"- {sentence}");
            }

            // Generate embeddings for each sentence
            var embeddings = new List<float[]>();

            for (int i = 0; i < testSentences.Length; i++)
            {
                string sentence = testSentences[i];
                Console.WriteLine($"Generating embedding for Sentence {i + 1}: {sentence}");

                // Generate embedding
                var embedding = await embeddingService.GenerateEmbeddingAsync(sentence);
                embeddings.Add(embedding.ToArray());

                Console.WriteLine($"Sentence {i + 1}: {sentence}");
            }

            // Calculate and display cosine similarity between sentence pairs
            Console.WriteLine("\nCosine Similarity Results:");

            double similarity1and2 = CalculateCosineSimilarity(embeddings[0], embeddings[1]);
            Console.WriteLine($"Similarity between Sentence 1 and Sentence 2: {similarity1and2:F4}");

            double similarity2and3 = CalculateCosineSimilarity(embeddings[1], embeddings[2]);
            Console.WriteLine($"Similarity between Sentence 2 and Sentence 3: {similarity2and3:F4}");

            double similarity3and1 = CalculateCosineSimilarity(embeddings[2], embeddings[0]);
            Console.WriteLine($"Similarity between Sentence 3 and Sentence 1: {similarity3and1:F4}");
        }

        /// <summary>
        /// Calculates the cosine similarity between two vectors.
        /// Cosine similarity ranges from -1 to 1:
        /// 1 = vectors point in the same direction (most similar)
        /// 0 = vectors are orthogonal (unrelated)
        /// -1 = vectors point in opposite directions (most dissimilar)
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <returns>Cosine similarity value between -1 and 1</returns>
        static double CalculateCosineSimilarity(ReadOnlyMemory<float> vector1, ReadOnlyMemory<float> vector2)
        {
            var span1 = vector1.Span;
            var span2 = vector2.Span;

            if (span1.Length != span2.Length)
            {
                throw new ArgumentException("Vectors must have the same length");
            }

            // Calculate dot product
            double dotProduct = TensorPrimitives.Dot(span1, span2);

            // Calculate magnitudes
            double magnitude1 = Math.Sqrt(TensorPrimitives.Dot(span1, span1));
            double magnitude2 = Math.Sqrt(TensorPrimitives.Dot(span2, span2));

            // Avoid division by zero
            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            // Calculate cosine similarity
            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
