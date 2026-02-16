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
using System.IO;
using Microsoft.Extensions.VectorData;

namespace SemanticKernelAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🤖 C# Semantic Kernel Agent Starting...\n");

            // Load configuration from environment variables
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(
                    "appsettings.json", 
                    optional: true, 
                    reloadOnChange: true)
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


            // Register an in-memory vector store with the kernel builder
            builder.Services.AddInMemoryVectorStore();

            // Build the kernel
            var kernel = builder.Build();

            // Obtain a collection for DocumentRecord named "sentences"
            var vectorStore = kernel.Services.GetRequiredService<VectorStore>();
            var documentCollection = vectorStore.GetCollection<string, DocumentRecord>("sentences");
            
            await documentCollection.EnsureCollectionExistsAsync();

            // Get the embedding service from the kernel

            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618

            Console.WriteLine("🔍 Embedding Inspector Lab\n");

            // Demo ingestion removed — load documents and create DocumentRecord instances here.
            // (Previously: created a test sentences array, generated embeddings, and upserted records.)

            Console.WriteLine("=== Loading Documents into Vector Database ===");

            var brochurePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "HealthInsuranceBrochure.md"));
            Console.WriteLine($"Loading {Path.GetFileName(brochurePath)}...");
            await LoadDocumentAsync(embeddingService, documentCollection, brochurePath);

            var handbookPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "EmployeeHandbook.md"));
            Console.WriteLine($"Loading {Path.GetFileName(handbookPath)}...");
            await LoadDocumentAsync(embeddingService, documentCollection, handbookPath);

            // Search for sentences similar to a query
            Console.WriteLine("\nSearching for sentences similar to a query...");

            // Define a search query
            string searchQuery = "The movie was excellent and entertaining.";

            // Perform the search
            await SearchSentencesAsync(embeddingService, documentCollection, searchQuery);

            Console.WriteLine("✅ Search complete.");

            Console.WriteLine("=== Semantic Search ===");
            while (true)
            {
                Console.Write("Enter a search query (or 'quit' to exit): ");
                var userInput = Console.ReadLine();

                // Check if the user wants to quit
                if (string.Equals(userInput, "quit", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                // Skip empty input
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine("Please enter a valid query.");
                    continue;
                }

                // Perform the search
                await SearchSentencesAsync(embeddingService, documentCollection, userInput);
            }
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
        static double CalculateCosineSimilarity(
            ReadOnlyMemory<float> vector1, 
            ReadOnlyMemory<float> vector2)
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

        /// <summary>
        /// Searches the vector store collection for the most similar sentences to a query.
        /// </summary>
        /// <param name="embeddingService">The embedding generation service.</param>
        /// <param name="sentenceCollection">The vector store collection.</param>
        /// <param name="query">The search query string.</param>
        /// <param name="topK">The number of top results to return (default is 3).</param>
        /// <returns>A task representing the asynchronous operation.</returns>

#pragma warning disable CS0618
        private static async Task SearchSentencesAsync(
            ITextEmbeddingGenerationService embeddingService,
            VectorStoreCollection<string, DocumentRecord> sentenceCollection,
            string searchQuery,
            int topK = 3)
        {
            // Generate embedding for the search query
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(searchQuery);

            Console.WriteLine($"\n🔍 Search Results for \"{searchQuery}\":\n");

            int rank = 1;

            await foreach (var result in sentenceCollection.SearchAsync(queryEmbedding, topK))
            {
                Console.WriteLine($"{rank}. [Score: {result.Score:F4}] {result.Record.Content}");
                rank++;
            }

            if (rank == 1)
            {
                Console.WriteLine("No results found.");
            }

            Console.WriteLine();
        }

        private static async Task LoadDocumentAsync(
            ITextEmbeddingGenerationService embeddingService,
            VectorStoreCollection<string, DocumentRecord> collection,
            string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    return;
                }

                var content = await File.ReadAllTextAsync(filePath);

                var embedding = await embeddingService.GenerateEmbeddingAsync(content);

                var record = new DocumentRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = Path.GetFileName(filePath),
                    Content = content,
                    Embedding = embedding,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await collection.UpsertAsync(records: new[] { record });

                Console.WriteLine($"✅ Upserted '{record.FileName}' ({record.Content.Length} chars).");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"❌ File not found: {filePath}");
            }
            catch (Exception ex)
            {
                var msg = ex.Message ?? string.Empty;
                if (msg.IndexOf("maximum context length", StringComparison.OrdinalIgnoreCase) >= 0
                    || msg.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("⚠️ This document is too large to embed as a single chunk.");
                    Console.WriteLine("Token limit exceeded. The embedding model can only process up to 8,191 tokens at once.");
                    Console.WriteLine("Solution: The document needs to be split into smaller chunks.");
                }
                else
                {
                    Console.WriteLine($"❌ Error loading '{filePath}': {ex.Message}");
                }
            }
        }
#pragma warning restore CS0618
    }

    public record DocumentRecord
    {
        [VectorStoreKey]
        public required string Id { get; init; }

        [VectorStoreData]
        public required string FileName { get; init; }

        [VectorStoreData]
        public required string Content { get; init; }

        [VectorStoreVector(1536)]
        public ReadOnlyMemory<float> Embedding { get; init; }

        [VectorStoreData]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
