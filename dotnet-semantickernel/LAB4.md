## Lab 4: Implementing Chunking Strategies with Semantic Kernel

In Lab 3, you discovered that the EmployeeHandbook.md is too large to embed as a single document. Now you'll solve this problem by implementing **document chunking** - splitting large documents into smaller, meaningful pieces that can each be embedded and searched independently.

Semantic Kernel provides built-in text chunking capabilities through the **Microsoft.SemanticKernel.Text** namespace. In this lab, you'll explore different chunking strategies and understand when to use each one.

### Understanding Chunking Strategies

Different chunking strategies serve different purposes:

1. **Fixed-Size Chunking** - Splits text into chunks of a specific character or token count. Simple but may break in the middle of sentences or concepts.

2. **Paragraph-Based Chunking** - Splits on paragraph boundaries, preserving complete thoughts. Works well for well-formatted documents.

3. **Sentence-Based Chunking** - Splits on sentence boundaries, grouping sentences together up to a maximum size. Balances granularity with coherence.

4. **Token-Based Chunking** - Uses actual token counts (as the AI model sees them) rather than character counts, providing precise control over embedding limits.

Each strategy has trade-offs between semantic coherence, chunk size consistency, and implementation complexity.

---

### Step 1: Install Text Chunking Dependencies

Semantic Kernel's text chunking functionality requires additional packages.

**Prompt 1: Add Text Chunking Package**
```
I need to add text chunking capabilities to my project. 
The TextChunker class is part of the Microsoft.SemanticKernel.Core package.
Show me the dotnet command to verify this package is installed (it should already be included as a dependency).
```

**What to Look For:** The TextChunker class is included in Microsoft.SemanticKernel.Core which is a dependency of the main Semantic Kernel package. You don't need to add additional packages.

**Documentation Reference:**
- [Vector Store Data Ingestion Guide](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/how-to/vector-store-data-ingestion)
- [TextChunker Class API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker)
- [Microsoft.SemanticKernel.Text Namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text)

---

### Step 2: Create a Chunking Helper Method

Before implementing different strategies, create a generic method to handle chunking and storing.

**Prompt 2: Create Chunked Document Loading Method**
```
Create an async method called LoadDocumentWithChunksAsync that:
- Takes parameters: embedding service, vector store collection, file path, and a list of text chunks
- Loops through each chunk with its index
- For each chunk:
  - Generates an embedding for the chunk text
  - Creates a DocumentRecord with:
    - Id: A GUID
    - FileName: The file name plus " (Chunk X/Total)" 
    - Content: The chunk text
    - Embedding: The generated embedding
    - CreatedAt: Current UTC time
  - Upserts the record to the collection
- Prints progress for each chunk processed
- Returns the total number of chunks stored

Include error handling for embedding failures.
```

**What to Look For:** This method will handle the common logic of processing chunks, regardless of which chunking strategy you use.

---

### Step 3: Implement Fixed-Size Character Chunking

The simplest strategy: split text every N characters.

**Prompt 3: Add Fixed-Size Chunking**
```
Using Microsoft.SemanticKernel.Text.TextChunker, create a method that:
- Reads the EmployeeHandbook.md file
- Uses TextChunker.SplitPlainTextLines to split the text into chunks
- Sets maxTokensPerLine to 1000
- Passes the chunks to LoadDocumentWithChunksAsync
- Prints statistics: number of chunks created, average chunk size

Update the ingestion of EmployeeHandbook to use this new method.

Add appropriate using statements for Microsoft.SemanticKernel.Text.
Note: `TextChunker` is marked for evaluation and may emit a diagnostic (SKEXP0050). If you see this, suppress it as appropriate (for example with a surrounding `#pragma warning disable SKEXP0050`/`#pragma warning restore SKEXP0050` or by configuring the project analyzer settings).
```
NOTE: We have to supress a warning because Semantic Kernel still has some beta features.

**What to Look For:** The document should be split into multiple chunks of roughly equal size.

**Documentation:**
- [TextChunker.SplitPlainTextLines Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker.splitplaintextlines)
- [TextChunker.SplitPlainTextParagraphs Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker.splitplaintextparagraphs)

**Do not test!**: Do not run at this point, our rate limiting will cause this to take a long time

### Step 4: Implement Paragraph-Based Chunking

### Step 4: Implement Paragraph-Based Chunking

A more semantic approach: split on paragraph boundaries while respecting size limits.

**Prompt 4: Add Paragraph Chunking**
```
Create a method to chunk by paragraphs:
- Read the EmployeeHandbook.md file
- Split the text on double newlines ("\n\n") to get paragraphs
- Group paragraphs together until reaching approximately 7000 characters per chunk
- Ensure no chunk exceeds 8000 characters (split large paragraphs if needed)
- Pass the chunks to LoadDocumentWithChunksAsync
- Compare the number of chunks and their sizes to fixed-size chunking

Print a comparison showing:
- Total chunks created
- Size of smallest and largest chunks
- Whether any chunks were split mid-paragraph
```

**What to Look For:** Chunks should align with document structure, making them more semantically coherent.

**Documentation:**
- [Vector Store Connectors Overview](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/)
- [Data Model Definition](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model)


### Test Step: Run and Validate
NOTE: Throttling will pause execution every 15 chunks. So it takes a while to run.

Before implementing paragraph-based chunking, run the app and validate the fixed-size ingestion worked. From the repository root run:

```powershell
dotnet run --project dotnet-semantickernel/dotnet-semantickernel.csproj
```

While the interactive CLI is running, try these example questions (answers should be present in the loaded documents):

- "What health insurance benefits are offered by the company?"
- "How do I enroll in benefits and what are the eligibility rules?"
- "What is the vacation / PTO policy for full-time employees?"
- "Does the company support remote work and what are the guidelines?"

The app should return matching document chunks ranked by relevance.

---

### Step 5: Implement Markdown-Aware Chunking

For markdown documents, respect the document structure (headings, sections).

**Prompt 5: Add Markdown Structure Chunking**
```
Create a method to chunk markdown by structure:
- Read the EmployeeHandbook.md file
- Use TextChunker.SplitMarkdownParagraphs to respect markdown structure
- Set chunk size to 1000 characters with 100 character overlap between chunks
- The overlap helps preserve context across chunk boundaries
- Pass the chunks to LoadDocumentWithChunksAsync
- Print examples of the first 3 chunks showing how headings are preserved
- Do not implement this chunking method yourself, use the library.

NOTE 1: You might believe this method is private, but the static method TextChunker.SplitMarkdownParagraphs does exist, so use it.
NOTE 2: The first parameter of this method is IEnumerable<string>, so you need to split the text into an array of lines
```

**What to Look For:** Chunks should preserve markdown structure, keeping headings with their content.

**Documentation:**
- [TextChunker.SplitMarkdownParagraphs Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker.splitmarkdownparagraphs)
- [TextChunker.SplitMarkDownLines Method](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker.splitmarkdownlines)

**Why Overlap Matters:** When you split "...benefits include health insurance. \n\n Health insurance covers..." at the paragraph break, overlap ensures both chunks contain "health insurance" context, improving search results.

---

### Step 6: Adding in an Agent to complete the RAG pattern!

Now that you have implemented chunking and vector search, let's complete the **Retrieval-Augmented Generation (RAG)** pattern by creating an AI agent that can search your documents and answer questions using the retrieved context.

Instead of just returning raw chunks from the vector database, an agent can:
1. Search the vector database for relevant chunks
2. Use those chunks as context when answering the user's question
3. Generate a natural language response based on the retrieved information

You'll implement this step-by-step by prompting GitHub Copilot to help you create the necessary components.

---

**Sub-Step 6.1: Create a Kernel Plugin with a Search Function**

The first step is to create a plugin function that the AI agent can call to search your document collection.

**Prompt 6.1: Create SearchDocuments Plugin Function**
```
Create a new class called DocumentSearchPlugin with a [KernelFunction] method called SearchDocuments.

The method should:
- Accept a string parameter called "query" 
- Use the embeddingService and documentsCollection (passed via constructor)
- Call the existing SearchDocumentsAsync method to perform the vector search
- Return a formatted string containing the top 3 search results
- Each result should include the chunk content and score
- Format the results as: "Result 1 (Score: X.XXXX): [content]\n\nResult 2..."

Add [Description] attributes to help the AI understand when to use this function:
- Class description: "Searches the company document repository for relevant information"
- Method description: "Searches for documents related to the given query and returns the most relevant matches"
- Parameter description: "The search query or question to find relevant documents for"

Note: You'll need to make the SearchDocumentsAsync method accessible to this plugin class.
```

**What to Look For:** 
- The plugin should use Semantic Kernel's plugin architecture with `[KernelFunction]` and `[Description]` attributes
- The search function should return formatted results that provide context to the LLM
- Consider making `SearchDocumentsAsync` static or accessible to the plugin class

**Documentation:**
- [Creating Semantic Kernel Plugins](https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/)
- [KernelFunction Attribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.kernelfunctionattribute)

---

**Sub-Step 6.2: Add Chat Completion Service**

Next, add a chat completion service that will power the conversational agent.

**Prompt 6.2: Add Chat Completion to Kernel**
```
Update the kernel builder to add a chat completion service:
- Use AddOpenAIChatCompletion with model "gpt-4o"
- Use the same openAIClient that was created for embeddings
- The service should be added to the builder before building the kernel

After building the kernel, get the IChatCompletionService from the kernel.
```

**What to Look For:**
- The chat completion service should use GitHub Models API (same endpoint as embeddings)
- Model "gpt-4o" provides good balance of quality and speed

**Documentation:**
- [Chat Completion Service](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/)

---

**Sub-Step 6.3: Register the Plugin with the Kernel**

Now register your search plugin so the agent can use it.

**Prompt 6.3: Register DocumentSearchPlugin**
```
Create an instance of DocumentSearchPlugin, passing the embeddingService and documentsCollection to the constructor.

Add the plugin to the kernel using kernel.Plugins.AddFromObject() with the plugin name "DocumentSearch".

Print a confirmation message showing that the plugin was registered.
```

**What to Look For:**
- The plugin should be instantiated with the required dependencies
- Plugin name should be descriptive and match the functionality

---

**Sub-Step 6.4: Replace the Interactive Search Loop with Agent Chat**

Finally, replace the simple vector search CLI with an agent-powered chat interface.

**Prompt 6.4: Create Agent Chat Loop**
```
Replace the existing semantic search while loop with a new chat interface that:

1. Creates a ChatHistory to track the conversation
2. Adds a system message explaining the agent's role:
   "You are a helpful assistant that answers questions about company policies, benefits, and procedures. 
    Use the SearchDocuments function to find relevant information from the company documents before answering. 
    Always cite which document chunks you used in your answer."
3. In a loop:
   - Prompt the user: "You: "
   - Read user input
   - Exit on "quit" or "exit"
   - Add the user message to chat history
   - Get a response from the chat completion service using kernel.InvokeAsync or GetChatMessageContentAsync
   - Enable automatic function calling so the agent can call SearchDocuments
   - Print the assistant's response
   - Add the assistant's response to chat history

The agent should automatically call the SearchDocuments function when needed to answer questions.

Print clear indicators showing:
- When the agent is calling the search function
- What query it's searching for
- The final response to the user
```

NOTE: You may need to modify the agent to remove logging of searches. I had to.

**What to Look For:**
- The agent should automatically invoke the SearchDocuments function when it needs information
- Conversation history should be maintained across multiple turns
- The agent's responses should reference the document chunks it retrieved

**Documentation:**
- [Auto Function Calling](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/)
- [Chat History](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/)

---

**Sub-Step 6.5: Test the Agent**

Run your application and test the agent-powered RAG system.

**Example Conversation:**
```
You: What health insurance benefits does the company offer?
[Agent calls SearchDocuments("health insurance benefits")]
Agent: Based on the company documentation, we offer comprehensive health insurance...

You: How do I enroll?
[Agent calls SearchDocuments("health insurance enrollment")]
Agent: To enroll in health insurance benefits, you need to...

You: What about dental coverage?
[Agent calls SearchDocuments("dental coverage")]
Agent: According to the benefits documentation...
```

**What to Look For:**
- The agent should automatically search documents when it needs information
- Responses should be natural and conversational, not just returning raw chunks
- The agent should maintain context across the conversation
- The agent should cite which document chunks it used

---

**Discussion: RAG vs. Simple Search**

After completing Step 6, compare the agent-powered RAG approach to the simple vector search from earlier steps:

**Simple Vector Search (Steps 1-5):**
- Returns raw document chunks
- User must read through chunks to find the answer
- No synthesis or summarization
- Limited to exact matches

**Agent-Powered RAG (Step 6):**
- Automatically searches when needed
- Synthesizes information from multiple chunks
- Provides natural language answers
- Can handle follow-up questions with conversation context
- Cites sources for transparency

This is the complete RAG pattern used in production AI applications!

---

### Discussion Questions

After completing Lab 4, consider:

1. **Chunk Size Trade-offs**: What happens if chunks are too small (100 characters)? Too large (5000 characters)? What's the "sweet spot"?

2. **Context Boundaries**: Did you notice any queries where the answer was split awkwardly across chunks? How could overlap help?

3. **Strategy Selection**: When would you choose fixed-size vs. markdown-aware chunking? What about for different document types (code, legal documents, chat logs)?

4. **Metadata Loss**: What information about document structure is lost when chunking? How could you preserve it?

5. **Search Quality**: Did chunking improve or worsen search results compared to searching full documents? Why?

---

### Extension Challenges

If you want to explore further:

**Challenge 1: Dynamic Chunk Sizing**
```
Ask Copilot: "Implement adaptive chunk sizing that creates smaller chunks for dense technical content and larger chunks for narrative text"
```

**Challenge 2: Hierarchical Chunking**
```
Ask Copilot: "Create a two-level chunking system: large chunks for context and smaller chunks for precision, storing both in the vector database with parent-child relationships"
```

**Challenge 3: Chunk Quality Scoring**
```
Ask Copilot: "Add a quality score to each chunk based on: completeness (starts/ends with sentence boundaries), coherence (single topic), and optimal size"
```

**Challenge 4: Smart Overlap**
```
Ask Copilot: "Implement intelligent overlap that only overlaps when chunks split mid-concept, detected by checking if text ends with sentence punctuation"
```

**Challenge 5: Cross-Chunk Context**
```
Ask Copilot: "When returning search results, if a chunk is ranked highly, also return the chunks immediately before and after it for better context"
```

---

### Key Takeaways

By completing Lab 4, you've learned:

✅ **Chunking is Essential** - Large documents must be split to work with embedding models

✅ **Multiple Strategies Exist** - Fixed-size, paragraph-based, and structure-aware chunking each have their place

✅ **Trade-offs Matter** - Chunk size, overlap, and boundary detection all affect search quality

✅ **Context Preservation** - Good chunking strategies preserve semantic meaning and document structure

✅ **Testing is Critical** - Always test chunking strategies with real queries to ensure quality

You now understand one of the core engineering challenges in RAG systems. In production applications, chunking strategy can make or break the user experience!

---

## What's Next?

You've now mastered the fundamentals of RAG:
- ✅ **Embeddings** - Converting text to vectors
- ✅ **Vector Storage** - Storing and indexing embeddings
- ✅ **Semantic Search** - Finding similar content
- ✅ **Chunking** - Handling large documents

The next step would be integrating RAG with an LLM to answer questions using retrieved context - but that's a topic for another lab!

**Great work completing this RAG engineering fundamentals unit!** 🎉