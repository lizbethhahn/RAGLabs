package com.codeyou.agent;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.core.credential.KeyCredential;
import com.microsoft.semantickernel.Kernel;
import com.microsoft.semantickernel.aiservices.openai.textembedding.OpenAITextEmbeddingGenerationService;
import com.microsoft.semantickernel.services.textembedding.TextEmbeddingGenerationService;
import io.github.cdimascio.dotenv.Dotenv;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class App {
    
    /**
     * Calculate cosine similarity between two vectors
     * Cosine similarity = (A · B) / (||A|| * ||B||)
     */
    private static double cosineSimilarity(List<Float> vectorA, List<Float> vectorB) {
        if (vectorA.size() != vectorB.size()) {
            throw new IllegalArgumentException("Vectors must have the same dimensions");
        }
        
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;
        
        for (int i = 0; i < vectorA.size(); i++) {
            dotProduct += vectorA.get(i) * vectorB.get(i);
            normA += vectorA.get(i) * vectorA.get(i);
            normB += vectorB.get(i) * vectorB.get(i);
        }
        
        return dotProduct / (Math.sqrt(normA) * Math.sqrt(normB));
    }
    public static void main(String[] args) {
        System.out.println("🤖 Java Semantic Kernel Agent Starting...\n");

        // Load environment variables from .env file
        Dotenv dotenv = Dotenv.configure()
                .ignoreIfMissing()
                .load();

        String githubToken = dotenv.get("GITHUB_TOKEN");
        if (githubToken == null || githubToken.isEmpty()) {
            githubToken = System.getenv("GITHUB_TOKEN");
        }

        if (githubToken == null || githubToken.isEmpty()) {
            System.out.println("❌ Error: GITHUB_TOKEN not found in environment variables.");
            System.out.println("Please create a .env file with your GitHub token:");
            System.out.println("GITHUB_TOKEN=your-github-token-here");
            return;
        }

        try {
            // Create OpenAI async client configured for GitHub Models
            OpenAIAsyncClient openAIAsyncClient = new OpenAIClientBuilder()
                    .endpoint("https://models.github.ai/inference")
                    .credential(new KeyCredential(githubToken))
                    .buildAsyncClient();

            // Build kernel with embedding service
            Kernel kernel = Kernel.builder()
                    .withAIService(TextEmbeddingGenerationService.class,
                            OpenAITextEmbeddingGenerationService.builder()
                                    .withOpenAIAsyncClient(openAIAsyncClient)
                                    .withModelId("text-embedding-3-small")
                                    .build())
                    .build();

            // Get the embedding service
            TextEmbeddingGenerationService embeddingService = 
                    kernel.getService(TextEmbeddingGenerationService.class);

            System.out.println("=== Embedding Inspector Lab ===\n");
            System.out.println("Generating embeddings for three sentences...\n");

            // Define the three test sentences
            List<String> sentences = Arrays.asList(
                "The canine barked loudly.",
                "The dog made a noise.",
                "The electron spins rapidly."
            );

            // Generate and display embeddings for each sentence
            List<List<Float>> vectors = new ArrayList<>();
            for (int i = 0; i < sentences.size(); i++) {
                var sentence = sentences.get(i);
                System.out.println("Sentence " + (i + 1) + ": \"" + sentence + "\"");
                
                // Generate embedding
                List<com.microsoft.semantickernel.services.textembedding.Embedding> embeddings = 
                        embeddingService.generateEmbeddingsAsync(Arrays.asList(sentence))
                        .block();
                
                if (embeddings != null && !embeddings.isEmpty()) {
                    var embedding = embeddings.get(0);
                    var vector = embedding.getVector();
                    vectors.add(vector);
                }
            }

            // Show the distances between the embeddings
            System.out.println("\n=== Embedding Vectors ===\n");
            for (int i = 0; i < vectors.size(); i++) {
                int current = i;
                int next = (i + 1) % vectors.size();
                double similarity = cosineSimilarity(vectors.get(current), vectors.get(next));
                System.out.printf("Cosine similarity between Sentence %d and Sentence %d: %.4f%n", 
                        current + 1, next + 1, similarity);
            }

            System.out.println("📊 Observations:");
            System.out.println("- Each embedding is just an array of floating-point numbers");
            System.out.println("- Sentences 1 and 2 (about dogs) will have similar values in many dimensions");
            System.out.println("- Sentence 3 (about electrons) will differ significantly from sentences 1 and 2");
            System.out.println("\nThis demonstrates that 'AI embeddings' are simply numerical vectors,");
            System.out.println("not magic—they represent semantic meaning as coordinates in high-dimensional space.");

        } catch (Exception e) {
            System.err.println("❌ Fatal error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
