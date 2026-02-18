using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
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
using System.Linq;
using SemanticKernelAgent;
using Microsoft.SemanticKernel.Text;
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

            // Register OpenAI chat completion service
            builder.AddOpenAIChatCompletion(
                modelId: "gpt-4o",
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
            // Get the chat completion service from the kernel
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
#pragma warning restore CS0618

            // Register DocumentSearchPlugin instance with the kernel
            var documentSearchPlugin = new DocumentSearchPlugin(embeddingService, documentCollection);
            kernel.Plugins.AddFromObject(documentSearchPlugin, "DocumentSearch");
            Console.WriteLine("✅ Plugin 'DocumentSearch' registered.");

            Console.WriteLine("🔍 Embedding Inspector Lab\n");

            // Demo ingestion removed — load documents and create DocumentRecord instances here.
            // (Previously: created a test sentences array, generated embeddings, and upserted records.)

            Console.WriteLine("=== Loading Documents into Vector Database ===");

            var brochurePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "HealthInsuranceBrochure.md"));
            Console.WriteLine($"Loading {Path.GetFileName(brochurePath)}...");
            await LoadDocumentAsync(embeddingService, documentCollection, brochurePath);

            var handbookPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "EmployeeHandbook.md"));
            Console.WriteLine($"Loading {Path.GetFileName(handbookPath)}...");
            await LoadAndChunkMarkdownAsync(embeddingService, documentCollection, handbookPath);

            // Search for sentences similar to a query
            Console.WriteLine("\nSearching for sentences similar to a query...");

            // // Define a search query
            // string searchQuery = "The movie was excellent and entertaining.";

            // // Perform the search
            // await SearchSentencesAsync(embeddingService, documentCollection, searchQuery);

            // Console.WriteLine("✅ Search complete.");

            Console.WriteLine("=== Chat Interface ===");

            var chatHistory = new ChatHistory();
            // System message describing agent role
            var systemMessage = @"You are a helpful assistant that answers questions about company policies, benefits, and procedures.
            Use the SearchDocuments function to find relevant information from the company documents before answering.
            Provide a concise summary (5-8 sentences max). Do not quote large passages. Briefly cite the chunk titles used.";
            chatHistory.AddSystemMessage(systemMessage);

            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

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

                // Add user message to history
                chatHistory.AddUserMessage(userInput);

                // For this simple flow, always call the SearchDocuments function first
                Console.WriteLine("\n🔎 Agent is calling SearchDocuments...");
                Console.WriteLine($"Searching for: {userInput}\n");

                var searchResults = await SearchDocumentsAsync(embeddingService, documentCollection, userInput, 3);

                // Format search results for the assistant
                var citationBuilder = new StringBuilder();
                if (searchResults.Count == 0)
                {
                    citationBuilder.AppendLine("No relevant document chunks found.");
                }
                else
                {
                    for (int i = 0; i < searchResults.Count; i++)
                    {
                        var r = searchResults[i];
                        var text = (r.Content ?? "").Replace("\r", " ").Replace("\n", " ");
                        if (text.Length > 200) text = text.Substring(0, 200) + "...";

                        citationBuilder.AppendLine($"Result {i + 1} (Score: {r.Score:F4})");
                        citationBuilder.AppendLine(text);
                        citationBuilder.AppendLine(new string('-', 60));
                        citationBuilder.AppendLine();                        
                    }
                }

                // Compose the assistant response using the search results
                var assistantResponse = new StringBuilder();
                assistantResponse.AppendLine("I searched the company documents and found the following relevant excerpts:");
                assistantResponse.AppendLine();
                assistantResponse.Append(citationBuilder.ToString());
                assistantResponse.AppendLine("Based on these excerpts, here is a short answer to your question:");
                assistantResponse.AppendLine();

                // Very simple heuristic: include first result content as an answer summary
                if (searchResults.Count > 0)
                {
                    var first = searchResults[0];
                    var summary = first.Content.Length > 400 ? first.Content.Substring(0, 400) + "..." : first.Content;
                    assistantResponse.AppendLine(summary);
                }
                else
                {
                    assistantResponse.AppendLine("I couldn't find information in the documents. Consider rephrasing your question.");
                }

                // Print the assistant response
                Console.WriteLine("\n--- Assistant ---");
                Console.WriteLine(assistantResponse.ToString());
                Console.WriteLine("--- End Assistant ---\n");

                // Add assistant response to history
                chatHistory.AddAssistantMessage(assistantResponse.ToString());
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
                var content = result.Record.Content ?? string.Empty;

                // Trim preview to avoid console spam
                const int previewLength = 400;
                if (content.Length > previewLength)
                {
                    content = content.Substring(0, previewLength) + "...";
                }

                Console.WriteLine($"{rank}. [Score: {result.Score:F4}] {result.Record.FileName}");
                Console.WriteLine(content);
                Console.WriteLine();

                rank++;
            }

            if (rank == 1)
            {
                Console.WriteLine("No results found.");
            }

            Console.WriteLine();
        }

        // Public helper to perform a vector search and return top results (content + score)
        public static async Task<List<(string Content, double Score)>> SearchDocumentsAsync(
            ITextEmbeddingGenerationService embeddingService,
            VectorStoreCollection<string, DocumentRecord> documentsCollection,
            string query,
            int topK = 3)
        {
            var results = new List<(string Content, double Score)>();
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);

            await foreach (var r in documentsCollection.SearchAsync(queryEmbedding, topK))
            {
                results.Add((r.Record.Content, r.Score ?? 0));
            }

            return results;
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

        private static async Task LoadAndChunkDocumentAsync(
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

                // Use TextChunker to split the document into chunks
                #pragma warning disable SKEXP0050
                var chunks = TextChunker.SplitPlainTextLines(content, maxTokensPerLine: 1000).ToList();
                #pragma warning restore SKEXP0050

                if (chunks.Count == 0)
                {
                    Console.WriteLine($"⚠️ No chunks created for {Path.GetFileName(filePath)}.");
                    return;
                }

                // Pass chunks to the loader that upserts each chunk
                await LoadDocumentWithChunksAsync(embeddingService, collection, Path.GetFileName(filePath), chunks);

                // Print statistics
                var totalChars = chunks.Sum(c => c.Length);
                var avgSize = (double)totalChars / chunks.Count;
                Console.WriteLine($"✅ Created {chunks.Count} chunks for '{Path.GetFileName(filePath)}'. Average chunk size: {avgSize:F1} chars.");
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
                    Console.WriteLine($"❌ Error chunking '{filePath}': {ex.Message}");
                }
            }
        }

        private static async Task LoadDocumentWithChunksAsync(
            ITextEmbeddingGenerationService embeddingService,
            VectorStoreCollection<string, DocumentRecord> collection,
            string fileName,
            IEnumerable<string> chunks)
        {
            var records = new List<DocumentRecord>();
            int index = 0;
            foreach (var chunk in chunks)
            {
                try
                {
                    var embedding = await embeddingService.GenerateEmbeddingAsync(chunk);
                    var record = new DocumentRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = fileName,
                        Content = chunk,
                        Embedding = embedding,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    records.Add(record);
                    index++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error embedding chunk {index} of '{fileName}': {ex.Message}");
                }
            }

            if (records.Count > 0)
            {
                await collection.UpsertAsync(records: records);
                Console.WriteLine($"✅ Upserted {records.Count} chunks for '{fileName}'.");
            }
        }

        private static async Task LoadAndChunkByParagraphsAsync(
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

                // Normalize newlines and split into paragraphs on double newlines
                var normalized = content.Replace("\r\n", "\n");
                var paragraphs = normalized
                    .Split(new string[] { "\n\n" }, StringSplitOptions.None)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList();

                var chunks = new List<string>();
                var current = new System.Text.StringBuilder();
                bool splitMidParagraph = false;

                foreach (var para in paragraphs)
                {
                    if (para.Length > 8000)
                    {
                        // split large paragraph into sub-parts <= 8000
                        int start = 0;
                        while (start < para.Length)
                        {
                            int take = Math.Min(8000, para.Length - start);
                            var part = para.Substring(start, take);
                            // if current has content, flush it first
                            if (current.Length > 0)
                            {
                                chunks.Add(current.ToString().Trim());
                                current.Clear();
                            }
                            chunks.Add(part);
                            start += take;
                            splitMidParagraph = true;
                        }
                        continue;
                    }

                    // If adding this paragraph would exceed ~7000 chars, flush current
                    if (current.Length + para.Length + 2 > 7000)
                    {
                        if (current.Length > 0)
                        {
                            chunks.Add(current.ToString().Trim());
                            current.Clear();
                        }
                    }

                    if (current.Length > 0)
                    {
                        current.Append("\n\n");
                    }
                    current.Append(para);
                }

                if (current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                }

                // Ensure no chunk exceeds 8000 chars; split if needed
                var finalChunks = new List<string>();
                foreach (var ch in chunks)
                {
                    if (ch.Length <= 8000)
                    {
                        finalChunks.Add(ch);
                    }
                    else
                    {
                        // split into 8000-char pieces
                        int start = 0;
                        while (start < ch.Length)
                        {
                            int take = Math.Min(8000, ch.Length - start);
                            finalChunks.Add(ch.Substring(start, take));
                            start += take;
                            splitMidParagraph = true;
                        }
                    }
                }

                // Create fixed-size chunks for comparison (7000-char blocks)
                var fixedChunks = new List<string>();
                int pos = 0;
                while (pos < normalized.Length)
                {
                    int take = Math.Min(7000, normalized.Length - pos);
                    fixedChunks.Add(normalized.Substring(pos, take));
                    pos += take;
                }

                // Print comparison statistics
                var sizes = finalChunks.Select(c => c.Length).ToList();
                var fixedSizes = fixedChunks.Select(c => c.Length).ToList();

                Console.WriteLine($"Paragraph-chunking: {finalChunks.Count} chunks. Smallest={sizes.Min()} chars. Largest={sizes.Max()} chars. Split mid-paragraph={splitMidParagraph}");
                Console.WriteLine($"Fixed-size (7000) chunking: {fixedChunks.Count} chunks. Smallest={fixedSizes.Min()} chars. Largest={fixedSizes.Max()} chars.");

                // Pass paragraph-based chunks to loader
                await LoadDocumentWithChunksAsync(embeddingService, collection, Path.GetFileName(filePath), finalChunks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in paragraph chunking for '{filePath}': {ex.Message}");
            }
        }

        private static async Task LoadAndChunkMarkdownAsync(
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
                var normalized = content.Replace("\r\n", "\n");
                var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None).AsEnumerable();

                // Use TextChunker.SplitMarkdownParagraphs to respect markdown structure
                #pragma warning disable SKEXP0050
                var chunks = TextChunker.SplitMarkdownParagraphs(lines, maxTokensPerParagraph: 1000, overlapTokens: 100).ToList();
                #pragma warning restore SKEXP0050

                if (chunks.Count == 0)
                {
                    Console.WriteLine($"⚠️ No chunks created for {Path.GetFileName(filePath)}.");
                    return;
                }

                Console.WriteLine($"Markdown-chunking: created {chunks.Count} chunks.");


                // Pass the markdown-aware chunks to the loader
                await LoadDocumentWithChunksAsync(embeddingService, collection, Path.GetFileName(filePath), chunks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in markdown chunking for '{filePath}': {ex.Message}");
            }
        }
        /// <summary>
        /// Loads a document by embedding and upserting a list of text chunks into the provided collection.
        /// Returns the number of chunks successfully stored.
        /// </summary>
        private static async Task<int> LoadDocumentWithChunksAsync(
            ITextEmbeddingGenerationService embeddingService,
            VectorStoreCollection<string, DocumentRecord> collection,
            string filePath,
            IReadOnlyList<string> chunks)
        {
            if (chunks == null || chunks.Count == 0)
            {
                Console.WriteLine("No chunks provided.");
                return 0;
            }

            var fileName = Path.GetFileName(filePath ?? string.Empty);
            int total = chunks.Count;
            int stored = 0;

            for (int i = 0; i < total; i++)
            {
                var chunkText = chunks[i] ?? string.Empty;
                int index = i + 1;

                try
                {
                    var embedding = await embeddingService.GenerateEmbeddingAsync(chunkText);

                    if (embedding.Length == 0)
                    {
                        Console.WriteLine($"⚠️ Embedding empty for chunk {index}/{total}. Skipping.");
                        continue;
                    }

                    var record = new DocumentRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = $"{fileName} (Chunk {index}/{total})",
                        Content = chunkText,
                        Embedding = embedding,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await collection.UpsertAsync(records: new[] { record });
                    stored++;

                    Console.WriteLine($"✅ Upserted chunk {index}/{total} ({chunkText.Length} chars).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to embed chunk {index}/{total}: {ex.Message}");
                    // continue to next chunk
                }
                await Task.Delay(4500); // delay to avoid rate limits (adjust as needed based on your embedding service's limits)
            }

            Console.WriteLine($"Finished upserting chunks: {stored}/{total} stored.");
            return stored;
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

#pragma warning disable CS0618
    [Description("Searches the company document repository for relevant information")]
    public class DocumentSearchPlugin
    {
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly VectorStoreCollection<string, DocumentRecord> _documentsCollection;

        public DocumentSearchPlugin(ITextEmbeddingGenerationService embeddingService, VectorStoreCollection<string, DocumentRecord> documentsCollection)
        {
            _embeddingService = embeddingService;
            _documentsCollection = documentsCollection;
        }

        [KernelFunction]
        [Description("Searches for documents related to the given query and returns the most relevant matches")]
        public async Task<string> SearchDocuments([Description("The search query or question to find relevant documents for")] string query)
        {
            var results = await Program.SearchDocumentsAsync(_embeddingService, _documentsCollection, query, 3);

            if (results == null || results.Count == 0)
            {
                return "No results found.";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var excerpt = (r.Content ?? "").Replace("\r", " ").Replace("\n", " ");

                if (excerpt.Length > 300) excerpt = excerpt.Substring(0, 300) + "...";
                sb.AppendFormat("Result {0} (Score: {1:F4}): {2}\n\n", i + 1, r.Score, excerpt);
            }
            return sb.ToString();
        }
    }
#pragma warning restore CS0618