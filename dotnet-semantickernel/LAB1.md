## Lab 1: Embedding Inspector

This lab introduces you to **text embeddings**, which are numerical representations of text that AI models use to understand semantic meaning.

#### Setup: Deploy the Embedding Model
1. Visit https://github.com/marketplace/models
2. Search for "text-embedding-3-small" in the models list
3. Click on the model and select "Get started" or "Deploy" to enable it
4. The model will now be available through your GitHub token (the same one you're using for chat)

#### Understanding Vector Similarity

**What are Embeddings?**

When an AI model processes text, it converts words into numbers. But instead of just counting words, it creates something called an **embedding** - a list of numbers (a vector) that captures the *meaning* of the text.

Think of it this way:
- Each sentence becomes a point in a multi-dimensional space (like coordinates on a map)
- Sentences with similar meanings are placed close together
- Sentences with different meanings are placed far apart

**What is Cosine Similarity?**

To measure how close two sentences are in meaning, we calculate the **cosine similarity** between their embedding vectors:
- A score of **1.0** means the sentences are very similar (almost identical in meaning)
- A score of **0.0** means they're completely unrelated
- A score of **-1.0** means they're opposite in meaning

It's like measuring the angle between two arrows:
- If the arrows point in the same direction → high similarity (close to 1)
- If the arrows point in different directions → low similarity (close to 0)

#### Build the Embedding Inspector

**Documentation Reference:**
- [Embedding Generation with Vector Stores](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/embedding-generation)
- [ITextEmbeddingGenerationService Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.embeddings.itextembeddinggenerationservice)

**Prompt 1: Add Embedding Service to Kernel**
```
Update the kernel builder to include text embedding generation:
- Add the text-embedding-3-small model using AddOpenAITextEmbeddingGeneration
- Use the same OpenAI client configured for GitHub Models
- Get the embedding service from the kernel after building it
```

**Prompt 2: Create Test Sentences**
```
Create an array of three test sentences:
1. "The canine barked loudly."
2. "The dog made a noise."
3. "The electron spins rapidly."

Add a header "Embedding Inspector Lab" with console output
```

**Prompt 3: Generate Embeddings**
```
Create a loop to:
- Generate an embedding for each sentence using embeddingService.GenerateEmbeddingAsync
- Store each embedding vector in a List<float[]>
- Print each sentence with a number (Sentence 1, Sentence 2, etc.)
```

**Prompt 4: Calculate Cosine Similarities**
```
Add code to calculate and display the cosine similarity between:
- Sentence 1 and Sentence 2
- Sentence 2 and Sentence 3
- Sentence 3 and Sentence 1

Use the CalculateCosineSimilarity() method and format the output to 4 decimal places
Display results with clear labels showing which sentences are being compared
```

> **Note:** The `CalculateCosineSimilarity` method in Program.cs was written by Copilot with a single prompt: "Write a method to calculate cosine similarity." If you'd like to practice, feel free to delete it and try generating it yourself with that prompt!

**Test Point**: Run the application using `dotnet run`. You should see:
- Each sentence printed
- Cosine similarity scores between each pair
- Sentences 1 and 2 (both about dogs) will have a HIGH similarity (around 0.7-0.9)
- Sentence 3 (about electrons) will have LOW similarity with sentences 1 and 2 (around 0.1-0.3)

#### Expected Output

```
🤖 C# Semantic Kernel Agent Starting...

=== Embedding Inspector Lab ===

Generating embeddings for three sentences...

Sentence 1: "The canine barked loudly."
Sentence 2: "The dog made a noise."
Sentence 3: "The electron spins rapidly."

=== Embedding Vectors ===

Cosine similarity between Sentence 1 and Sentence 2: ...
Cosine similarity between Sentence 2 and Sentence 3: ...
Cosine similarity between Sentence 3 and Sentence 1: ...
```

Write down these similarities. What observations do you see?

#### Experiment: Modify Sentences to See How Similarity Changes

Now that you understand the basics, try modifying the sentences to see how the similarity scores change! Here are several experiments you can try:

**Experiment 1: Same Sentence in Different Languages**
```csharp
string[] sentences = new[]
{
    "The dog barked loudly.",
    "El perro ladró fuerte.",  // Spanish
    "Le chien a aboyé fort."   // French
};
```
**What to expect:** High similarity scores! Embeddings understand meaning across languages.

**Experiment 2: Change Just One Word**
```csharp
string[] sentences = new[]
{
    "The cat sat on the mat.",
    "The dog sat on the mat.",
    "The cat sat on the hat."
};
```
**What to expect:** Very high similarity (around 0.9+) because most words are identical.

**Experiment 3: Add Adjectives and Details**
```csharp
string[] sentences = new[]
{
    "I like pizza.",
    "I really love eating delicious, hot pizza.",
    "Pizza is good."
};
```
**What to expect:** Still high similarity - the core meaning (liking pizza) is the same.

**Experiment 4: Opposite Meanings**
```csharp
string[] sentences = new[]
{
    "The movie was excellent and entertaining.",
    "The movie was terrible and boring.",
    "I enjoyed watching the film."
};
```
**What to expect:** Sentences 1 and 2 will have moderate similarity (same topic: movies) but lower than 1 and 3.

**Experiment 5: Technical vs. Casual Language**
```csharp
string[] sentences = new[]
{
    "The precipitation is expected to commence shortly.",
    "It's going to rain soon.",
    "The weather forecast indicates imminent rainfall."
};
```
**What to expect:** High similarity - all mean the same thing despite different vocabulary.

**Experiment 6: Different Topics**
```csharp
string[] sentences = new[]
{
    "I love programming in C#.",
    "Chocolate cake is delicious.",
    "The ocean is very deep."
};
```
**What to expect:** Low similarity across all pairs - completely different topics.

**Your Turn!**

Choose 2-3 experiments above and modify the `sentences` array in your Program.cs. Run the program each time and observe how the similarity scores change. Write down:
1. Which experiment you tried
2. What the similarity scores were
3. Whether the results matched your expectations

**Discussion Questions:**
- Why do you think sentences in different languages can have high similarity?
- What happens to similarity when you keep the topic the same but change the sentiment (positive vs. negative)?
- How might these embeddings be useful in a real application (search engines, recommendation systems, etc.)?
