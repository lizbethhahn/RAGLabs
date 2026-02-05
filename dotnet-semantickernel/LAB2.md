
## Lab 2: Storing Embeddings in a Vector Database

In Lab 1, you generated embeddings and calculated similarity scores. But what if you have hundreds or thousands of sentences? You need a way to store them and search through them efficiently. That's where **vector databases** come in.

In this lab, you'll use Semantic Kernel's **VolatileVectorStore** - an in-memory vector database perfect for learning and development. You'll store sentences along with their embeddings, then build a CLI search feature that finds the most similar sentences to a user's query.

#### What is VolatileVectorStore?

VolatileVectorStore is Semantic Kernel's built-in in-memory vector database. "Volatile" means the data only exists while your program is running - it's not saved to disk. This makes it perfect for:
- Learning and experimentation
- Testing vector search functionality
- Prototyping RAG applications
- Small datasets that can fit in memory

For production applications, you'd use persistent vector databases like Chroma, Pinecone, or Qdrant, but the concepts are the same. Remember to run your code after each step. You want to incrementally make changes and test so you can be a good AI pair programmer!

**📚 Documentation Resources:**
- [Semantic Kernel Vector Store Overview](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/)
- [Using Vector Stores in Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model)
- [In-Memory Vector Store Connector](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.InMemory/)

---

### Step 1: Define Your Data Model

Before storing data in a vector database, you need to define what each "record" looks like. In this case, each record will have:
- A unique ID
- The original sentence text
- The embedding vector
- Optionally, metadata like when it was added

**📚 Reference:** [Defining Your Data Model](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model) - Learn about vector store attributes and record definitions.

**Prompt 1: Create a Data Model Class**
```
Create a C# record class called SentenceRecord with the following properties:
- Id of type string with [VectorStoreKey] attribute
- Text of type string with [VectorStoreData] attribute
- Embedding of type ReadOnlyMemory<float> with [VectorStoreVector] attribute and dimension of 1536
- CreatedAt of type DateTimeOffset with [VectorStoreData] attribute

Include the necessary using statements for Microsoft.SemanticKernel.Data attributes.
```

**What to Look For:** GitHub Copilot should create a record class that defines the structure of data you'll store in the vector database. The attributes tell Semantic Kernel which property is the key, which is the vector, and which is regular data.

---

### Step 2: Initialize the Vector Store

Now you need to create an instance of the VolatileVectorStore and get a collection to work with.

**NOTE:** You may need to install dependencies. Check the documentation and/or ask AI to help. Your code may not work without this.

**📚 Required Documentation:**
- [Vector Store Connectors Package](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.InMemory/) - You may need to install this NuGet package
- [Out-of-the-box Vector Store Connectors](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/) - Overview of available vector store implementations

**Prompt 2: Register and Obtain an In-Memory Vector Store**
```
After building the kernel and getting the embedding service, register an in-memory vector store with the kernel builder then obtain a collection for `SentenceRecord` named "sentences".
```

**What to Look For:** The code should create a vector store instance and obtain a collection that you can add records to and search through. 

NOTE: This step will probably mess up, you will have to use documentation to get it right since this code has changed a lot.

---

### Step 3: Store Sentences with Their Embeddings

Instead of just printing the embeddings like in Lab 1, you'll now store them in the vector database.

**Prompt 3: Generate and Store Sentence Embeddings**
```
Replace the current embedding generation code with the following logic:
1. Create a list to store SentenceRecord objects
2. Loop through each sentence in the sentences array
3. For each sentence:
   - Generate its embedding using the embedding service
   - Create a new SentenceRecord with:
     - Id: A GUID converted to string
     - Text: The original sentence
     - Embedding: The generated embedding vector
     - CreatedAt: The current UTC time
   - Add the record to the list
4. Use the collection's UpsertBatchAsync method to store all records
5. Print a confirmation message showing how many sentences were stored
```

**What to Look For:** This should replace the code that just calculated similarities. Now you're persisting the data in the vector store for later retrieval.

---

### Step 4: Build a Semantic Search Function

Now comes the powerful part - searching! You'll create a function that takes a user's query, converts it to an embedding, and finds the most similar sentences in the database.

**📚 Reference:** [Searching with Vector Stores](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/vector-search) - Learn about vector similarity search methods.

**Prompt 4: Create a Semantic Search Method**
```
Create a new async method called SearchSentencesAsync that:
- Takes parameters: the embedding service, the vector store collection, a search query string, and a top K results count (default to 3)
- Generates an embedding for the search query
- Performs a vector search on the collection using SearchAsync with the query embedding
- Returns the top K most similar results
- Each result should include the sentence text and its similarity score
- Print the results with formatting showing rank, similarity score (formatted to 4 decimals), and the sentence text
```

**What to Look For:** GitHub Copilot should create a method that performs vector similarity search and displays results ranked by relevance.

---

### Step 5: Create an Interactive CLI Search Loop

Finally, make your application interactive so users can type queries and see results in real-time.

**Prompt 5: Add Interactive Search CLI**
```
After storing the sentences, add an interactive loop that:
1. Displays a header "=== Semantic Search ===" 
2. Prompts the user to "Enter a search query (or 'quit' to exit): "
3. Reads the user's input
4. If the user types "quit" or "exit", break the loop
5. If the input is empty, continue to the next iteration
6. Otherwise, call the SearchSentencesAsync method with the user's query
7. Display the results
8. Repeat the loop

Include helpful messages and formatting to make the CLI user-friendly.
```

**What to Look For:** Your application should now have an interactive search interface where users can enter queries and see similar sentences.

Reset your sentences to the original list:
```csharp
string[] sentences = new[]
{
    "The canine barked loudly.",
    "The dog made a noise.",
    "The electron spins rapidly."
};
```
---

### Step 6: Test Your Semantic Search

**Test Point**: Run the application using `dotnet run`. You should see:

1. Confirmation that sentences were stored in the vector database
2. A prompt asking you to enter a search query
3. Ability to type queries like:
   - "animal sounds" (should find sentences about dogs barking)
   - "physics" (should find the electron sentence)
   - "pet making noise" (should find dog-related sentences)
   - "scientific particles" (should find the electron sentence)

**Try These Queries:**
- "puppy barking" - Should match the dog sentences even though "puppy" wasn't in the original text
- "subatomic particles rotating" - Should match the electron sentence
- "cat meowing" - Should still match dog sentences (both are animals making sounds)

**What You'll Notice:**
- You don't need exact keyword matches
- The search understands semantic meaning
- Results are ranked by similarity (closest matches first)
- Related concepts cluster together even with different wording

---

### Step 7: Expand Your Dataset

Now that you have a working vector search system, add more sentences to make it more interesting!

**Prompt 6: Add More Diverse Sentences**
```
Expand the sentences array to include at least 10-15 sentences covering different topics such as:
- Animals and pets
- Science and physics
- Food and cooking
- Sports and activities
- Weather and nature
- Technology and programming

Keep the sentences varied so you can test how well semantic search groups related concepts.
```

**Example Dataset You Might Create:**
```csharp
string[] sentences = new[]
{
    "The canine barked loudly.",
    "The dog made a noise.",
    "The electron spins rapidly.",
    "I love eating pizza with extra cheese.",
    "The basketball player scored a three-pointer.",
    "Rain is forecasted for tomorrow afternoon.",
    "Python is a popular programming language.",
    "The kitten purred softly on the couch.",
    "Quantum mechanics explains particle behavior.",
    "Homemade pasta tastes better than store-bought.",
    "The soccer match ended in a tie.",
    "Clouds are forming over the mountains.",
    "JavaScript runs in web browsers.",
    "Puppies need lots of attention and exercise.",
    "Atoms are made of protons, neutrons, and electrons."
};
```

**Test Again**: Run the application and try queries like:
- "pets" - Should find dog, cat, puppy, and kitten sentences
- "science" - Should find physics, quantum mechanics, and atom sentences
- "Italian food" - Should find pizza and pasta sentences
- "code" - Should find programming language sentences

---

### Expected Output

```
🤖 C# Semantic Kernel Agent Starting...

=== Embedding Inspector Lab ===

Storing 15 sentences in the vector database...
✅ Successfully stored 15 sentences

=== Semantic Search ===

Enter a search query (or 'quit' to exit): pets

🔍 Search Results for "pets":

1. [Score: ...] The canine barked loudly.
2. [Score: ...] The dog made a noise.
3. [Score: ...] Puppies need lots of attention and exercise.

Enter a search query (or 'quit' to exit): science

🔍 Search Results for "science":

1. [Score: ...] Quantum mechanics explains particle behavior.
2. [Score: ...] Atoms are made of protons, neutrons, and electrons.
3. [Score: ...] The electron spins rapidly.

Enter a search query (or 'quit' to exit): quit

👋 Goodbye!
```

---

### Discussion Questions

After completing Lab 2, consider:

1. **Keyword vs. Semantic Search**: How does vector search differ from traditional keyword search? What advantages does it offer?

2. **Similarity Scores**: Why do some queries return higher similarity scores than others? What factors affect the score?

3. **False Positives**: Did any of your queries return unexpected results? Why might that happen?

4. **Scaling Considerations**: The VolatileVectorStore keeps everything in memory. What challenges would arise with 1 million sentences? 

5. **Real-World Applications**: Where would semantic search be useful in applications you use daily (search engines, customer support, content recommendations)?

---

### Extension Challenges

If you finish early or want to go deeper:

**Challenge 1: Add Metadata Filtering**
```
Modify the SentenceRecord to include a Category property (e.g., "animals", "science", "food").
Update the search to allow filtering by category.
Ask Copilot: "Add category-based filtering to the vector search to only search within specific categories"
```

**Challenge 2: Hybrid Search**
```
Combine vector similarity search with keyword matching.
Ask Copilot: "Implement hybrid search that combines vector similarity with keyword matching for better precision"
```

**Challenge 3: Threshold Filtering**
```
Only show results above a minimum similarity score (e.g., 0.7).
Ask Copilot: "Add a similarity threshold parameter to filter out low-confidence results"
```

**Challenge 4: Export and Import**
```
Save the vector store to a JSON file and reload it on startup.
Ask Copilot: "Add functionality to serialize the vector store to JSON and reload it when the application starts"
```

---
