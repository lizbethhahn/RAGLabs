using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.Configuration;
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

            var kernel = builder.Build();
            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

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
