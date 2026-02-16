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

            // Obtain a collection for SentenceRecord named "sentences"
            var vectorStore = kernel.Services.GetRequiredService<VectorStore>();
            var sentenceCollection = vectorStore.GetCollection<string, SentenceRecord>("sentences");
            
            await sentenceCollection.EnsureCollectionExistsAsync();

            // Get the embedding service from the kernel

            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618

            Console.WriteLine("🔍 Embedding Inspector Lab\n");

            // Define test sentences
            string[] testSentences = {
                // Animals and pets
                "Dogs are loyal and friendly animals.",
                "Cats love to climb and explore their surroundings.",

                // Science and physics
                "Gravity is a force that attracts objects toward each other.",
                "The speed of light is approximately 299,792 kilometers per second.",

                // Food and cooking
                "Pizza is a popular dish made with cheese and tomato sauce.",
                "Baking a cake requires flour, eggs, and sugar.",

                // Sports and activities
                "Soccer is played by two teams of eleven players each.",
                "Swimming is a great way to stay fit and healthy.",

                // Weather and nature
                "Rainbows appear when sunlight passes through raindrops.",
                "Thunderstorms often bring heavy rain and lightning.",

                // Technology and programming
                "Artificial intelligence is transforming the tech industry.",
                "Learning to code can open up many career opportunities."
            };

            // Create a list to store SentenceRecord objects
            var sentenceRecords = new List<SentenceRecord>();

            // Loop through each sentence in the sentences array
            foreach (var sentence in testSentences)
            {
                Console.WriteLine($"Generating embedding for: {sentence}");

                // Generate embedding
                var embedding = await embeddingService.GenerateEmbeddingAsync(sentence);

                // Create a new SentenceRecord
                var record = new SentenceRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = sentence,
                    Embedding = embedding,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                // Add the record to the list
                sentenceRecords.Add(record);
            }

            // Use the collection's UpsertBatchAsync method to store all records
            await sentenceCollection.UpsertAsync(records: sentenceRecords);

            // Print a confirmation message showing how many sentences were stored
            Console.WriteLine($"✅ {sentenceRecords.Count} sentences stored in the vector store.");

            // Calculate and display cosine similarity between sentence pairs
            Console.WriteLine("\nCosine Similarity Results:");

            double similarity1and2 = CalculateCosineSimilarity(
                sentenceRecords[0].Embedding, 
                sentenceRecords[1].Embedding);
            Console.WriteLine($"Similarity between Sentence 1 and Sentence 2: {similarity1and2:F4}");

            double similarity2and3 = CalculateCosineSimilarity(
                sentenceRecords[1].Embedding, 
                sentenceRecords[2].Embedding);
            Console.WriteLine($"Similarity between Sentence 2 and Sentence 3: {similarity2and3:F4}");

            double similarity3and1 = CalculateCosineSimilarity(
                sentenceRecords[2].Embedding, 
                sentenceRecords[0].Embedding);
            Console.WriteLine($"Similarity between Sentence 3 and Sentence 1: {similarity3and1:F4}");

            // Search for sentences similar to a query
            Console.WriteLine("\nSearching for sentences similar to a query...");

            // Define a search query
            string searchQuery = "The movie was excellent and entertaining.";

            // Perform the search
            await SearchSentencesAsync(embeddingService, sentenceCollection, searchQuery);

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
                await SearchSentencesAsync(embeddingService, sentenceCollection, userInput);
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
            VectorStoreCollection<string, SentenceRecord> sentenceCollection,
            string searchQuery,
            int topK = 3)
#pragma warning restore CS0618
        {
            // Generate embedding for the search query
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(searchQuery);

            Console.WriteLine($"\n🔍 Search Results for \"{searchQuery}\":\n");

            int rank = 1;

            await foreach (var result in sentenceCollection.SearchAsync(queryEmbedding, topK))
            {
                Console.WriteLine($"{rank}. [Score: {result.Score:F4}] {result.Record.Text}");
                rank++;
            }

            if (rank == 1)
            {
                Console.WriteLine("No results found.");
            }

            Console.WriteLine();
        }
    }

    public record SentenceRecord
    {
        [VectorStoreKey]
        public required string Id { get; init; }

        [VectorStoreData]
        public required string Text { get; init; }

        [VectorStoreVector(1536)]
        public ReadOnlyMemory<float> Embedding { get; init; }

        [VectorStoreData]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
