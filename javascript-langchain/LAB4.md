## Lab 4: Implementing Chunking Strategies with LangChain

In Lab 3, you discovered that the EmployeeHandbook.md is too large to embed as a single document. Now you'll solve this problem by implementing **document chunking** - splitting large documents into smaller, meaningful pieces that can each be embedded and searched independently.

LangChain provides built-in text chunking capabilities through the **langchain_text_splitters** module. In this lab, you'll explore different chunking strategies and understand when to use each one.

### Understanding Chunking Strategies

Different chunking strategies serve different purposes:

1. **Fixed-Size Chunking** - Splits text into chunks of a specific character or token count. Simple but may break in the middle of sentences or concepts.

2. **Paragraph-Based Chunking** - Splits on paragraph boundaries, preserving complete thoughts. Works well for well-formatted documents.

3. **Sentence-Based Chunking** - Splits on sentence boundaries, grouping sentences together up to a maximum size. Balances granularity with coherence.

4. **Token-Based Chunking** - Uses actual token counts (as the AI model sees them) rather than character counts, providing precise control over embedding limits.

Each strategy has trade-offs between semantic coherence, chunk size consistency, and implementation complexity.

---

### Step 1: Install Text Chunking Dependencies

LangChain's text chunking functionality is available through the **@langchain/textsplitters** package.

**Prompt 1: Add Text Chunking Package**
```
I need to add text chunking capabilities to my JavaScript project.
Add the @langchain/textsplitters package to package.json.
Show me the import statements I'll need for text splitting in LangChain.
```

**What to Look For:** The @langchain/textsplitters package should be added to package.json. You'll import various splitters like CharacterTextSplitter, RecursiveCharacterTextSplitter, and MarkdownHeaderTextSplitter.

**Documentation Reference:**
- [LangChain Text Splitters](https://js.langchain.com/docs/how_to/#text-splitters)
- [Text Splitters API Reference](https://js.langchain.com/docs/modules/data_connection/document_transformers/)
- [Document Transformers](https://js.langchain.com/docs/integrations/document_transformers/)

---

### Step 2: Create a Chunking Helper Function

Before implementing different strategies, create a generic function to handle chunking and storing.

**Prompt 2: Create Chunked Document Loading Function**
```
Create an async function called loadDocumentWithChunks that:
- Takes parameters: vectorStore, filePath, and an array of LangChain Document objects (chunks)
- Loops through each chunk with its index
- For each chunk:
  - Updates the chunk's metadata to include:
    - fileName: The file name plus " (Chunk X/Total)"
    - createdAt: Current timestamp using new Date().toISOString()
    - chunkIndex: The chunk number
  - Adds the chunk to the vector store using await vectorStore.addDocuments()
- Prints progress for each chunk processed
- Returns the total number of chunks stored

Include error handling using try-catch.
```

**What to Look For:** This function will handle the common logic of processing chunks, regardless of which chunking strategy you use.

---

### Step 3: Implement Fixed-Size Character Chunking

The simplest strategy: split text every N characters.

**Prompt 3: Add Fixed-Size Chunking**
```
Create an async function that:
- Reads the EmployeeHandbook.md file using fs.readFileSync() with 'utf-8' encoding
- Uses CharacterTextSplitter from @langchain/textsplitters with:
  - chunkSize: 1000
  - chunkOverlap: 0
  - separator: " " (split on spaces to avoid breaking words)
- Calls createDocuments([text]) to generate Document objects
- Passes the chunks to loadDocumentWithChunks
- Prints statistics: number of chunks created, average chunk size

Import CharacterTextSplitter from '@langchain/textsplitters'.
Import fs from 'fs'.
```

**What to Look For:** The document should be split into multiple chunks of roughly equal size.

**Documentation:**
- [CharacterTextSplitter API](https://js.langchain.com/docs/modules/data_connection/document_transformers/)
- [Node.js File System](https://nodejs.org/api/fs.html)

**Test Point**: Run the application using `node app.js`. You should see the EmployeeHandbook successfully split and loaded.

**Expected Output:**
```
📄 Loading EmployeeHandbook.md with fixed-size chunking...
Processing chunk 1/15...
Processing chunk 2/15...
...
✅ Successfully loaded 15 chunks from EmployeeHandbook.md
Average chunk size: 1,247 characters
```

---

### Step 4: Implement Paragraph-Based Chunking

A more semantic approach: split on paragraph boundaries while respecting size limits.

**Prompt 4: Add Paragraph Chunking**
```
Create an async function to chunk by paragraphs:
- Read the EmployeeHandbook.md file
- Use RecursiveCharacterTextSplitter from @langchain/textsplitters with:
  - chunkSize: 1500
  - chunkOverlap: 0
  - separators: ["\n\n", "\n", " ", ""] (splits on paragraphs first, then newlines, then spaces)
- This splitter tries to keep paragraphs together while respecting the size limit
- Call createDocuments([text]) to generate chunks
- Pass the chunks to loadDocumentWithChunks
- Compare the number of chunks and their sizes to fixed-size chunking

Print a comparison showing:
- Total chunks created
- Size of smallest and largest chunks
- How many chunks start with a newline (indicating paragraph preservation)

Import RecursiveCharacterTextSplitter from '@langchain/textsplitters'.
```

**What to Look For:** Chunks should align with document structure, making them more semantically coherent.

**Documentation:**
- [RecursiveCharacterTextSplitter API](https://js.langchain.com/docs/modules/data_connection/document_transformers/)
- [Text Splitters How-to Guide](https://js.langchain.com/docs/how_to/#text-splitters)

**Test Point**: Run this strategy and compare results with fixed-size chunking.

---

### Step 5: Implement Markdown-Aware Chunking

For markdown documents, respect the document structure (headings, sections).

**Prompt 5: Add Markdown Structure Chunking**
```
Create an async function to chunk markdown by structure:
- Read the EmployeeHandbook.md file
- Use MarkdownHeaderTextSplitter from @langchain/textsplitters with:
  - headersToSplitOn: [{"#": "Header 1"}, {"##": "Header 2"}, {"###": "Header 3"}]
- This splits the document on markdown headers, preserving structure
- Then apply RecursiveCharacterTextSplitter with:
  - chunkSize: 1500
  - chunkOverlap: 200
- The overlap helps preserve context across chunk boundaries
- Pass the chunks to loadDocumentWithChunks
- Print examples of the first 3 chunks showing how headings are preserved in metadata

Import MarkdownHeaderTextSplitter from '@langchain/textsplitters'.
Note: The header information will be stored in each chunk's metadata.
```

**What to Look For:** Chunks should preserve markdown structure, keeping headings with their content. Check the metadata for header information.

**Documentation:**
- [MarkdownHeaderTextSplitter API](https://js.langchain.com/docs/modules/data_connection/document_transformers/)
- [Markdown Splitting Guide](https://js.langchain.com/docs/how_to/#text-splitters)

**Why Overlap Matters:** When you split "...benefits include health insurance. \n\n Health insurance covers..." at the paragraph break, overlap ensures both chunks contain "health insurance" context, improving search results.

---

### Step 6: Compare Chunking Strategies

Now that you have multiple strategies implemented, compare their effectiveness.

**Prompt 6: Create Strategy Comparison**
```
Create an async function that runs all three chunking strategies on EmployeeHandbook.md and compares:
- Total number of chunks created
- Average chunk size in characters
- Minimum and maximum chunk sizes
- Number of chunks that have header metadata (for markdown-aware)
- Estimated token count per chunk (rough estimate: chars / 4)

Display the results in a formatted table using JavaScript template literals:
Strategy | Chunks | Avg Size | Min Size | Max Size | Headers
Display this comparison before actually loading any chunks into the vector store.

Use template literals for table formatting.
```

**What to Look For:** You'll see how different strategies create different chunk distributions.

**Test Point**: Run the comparison and observe the differences.

**Expected Output (example):**
```
=== Chunking Strategy Comparison ===

Strategy              | Chunks | Avg Size | Min Size | Max Size | Headers
--------------------- | ------ | -------- | -------- | -------- | --------
Fixed Size           |   15   |  1,247   |   1,100  |  1,350   |    N/A
Paragraph-Based      |   12   |  1,556   |    800   |  2,000   |    N/A
Markdown-Aware       |   18   |  1,039   |    650   |  1,500   |   14
```

---

### Step 7: Choose and Implement Your Final Strategy

Based on your comparison, select the best strategy for the Employee Handbook.

**Prompt 7: Implement Final Chunking Solution**
```
Based on the comparison results, implement the markdown-aware chunking strategy as the final solution.
Update the document loading code to:
1. Recreate the vector store (MemoryVectorStore) to clear previous data
2. Load HealthInsuranceBrochure.md as-is using the loadDocument function from Lab 3 (it's small enough)
3. Load EmployeeHandbook.md using markdown-aware chunking with overlap
4. Display a summary showing:
   - Total number of documents/chunks in the vector store
   - Source files loaded
   - Number of chunks per file

Then add the interactive search loop from Lab 2 back in so users can search across both documents.
Use readline or prompt-sync for user queries and display results with similarity scores and chunk information.
```

**What to Look For:** Your application should now handle both small documents (load as-is) and large documents (chunk first) intelligently.

---

### Step 8: Test Semantic Search Across Chunks

Now test whether chunking improves search quality.

**Test Queries to Try:**

**Query 1: "health insurance benefits"**
- Should return relevant chunks from both documents
- Notice if chunks from EmployeeHandbook provide more specific details than the Brochure

**Query 2: "vacation policy"**
- Should return chunks from EmployeeHandbook that discuss vacation/PTO
- Observe which chunks are most relevant

**Query 3: "dental coverage options"**
- Should return chunks from both documents
- Notice how overlap helps - you might see adjacent chunks that both mention dental

**Query 4: "remote work policy"**
- Should return chunks discussing remote/hybrid work from the Handbook
- Test if context is preserved within the chunk

**Observations to Make:**
1. Do chunks contain enough context to be useful standalone?
2. Are answers spread across multiple chunks? (This shows why RAG systems retrieve multiple chunks)
3. Does overlap help or create duplicate results?
4. Are some chunks too small or too large?

---

### Expected Output

```
🤖 JavaScript LangChain Agent Starting...

=== Loading Documents into Vector Database ===

Loading HealthInsuranceBrochure.md...
✅ Successfully loaded HealthInsuranceBrochure.md as single document (2,456 characters)

Loading EmployeeHandbook.md with markdown-aware chunking...
📄 Creating chunks with 1500 character max and 200 character overlap...
Processing chunk 1/18 (Section: Introduction)...
Processing chunk 2/18 (Section: Benefits Overview)...
...
✅ Successfully loaded 18 chunks from EmployeeHandbook.md

📊 Vector Database Summary:
- Total records: 19
- Documents: 2 (HealthInsuranceBrochure.md, EmployeeHandbook.md)
- Chunks: 18 from EmployeeHandbook.md

=== Semantic Search ===

Enter a search query (or 'quit' to exit): health insurance benefits

🔍 Search Results for "health insurance benefits":

1. [Score: 0.8923] [HealthInsuranceBrochure.md] TechCorp Solutions Health & Wellness Benefits...
2. [Score: 0.8756] [EmployeeHandbook.md (Chunk 3/18)] ## Health Insurance Plans\n\nTechCorp offers comprehensive...
3. [Score: 0.8234] [EmployeeHandbook.md (Chunk 4/18)] ### Enrollment and Eligibility\n\nAll full-time employees...

Enter a search query (or 'quit' to exit): remote work policy

🔍 Search Results for "remote work policy":

1. [Score: 0.9012] [EmployeeHandbook.md (Chunk 12/18)] ## Remote Work and Hybrid Options\n\nTechCorp supports...
2. [Score: 0.8445] [EmployeeHandbook.md (Chunk 13/18)] Employees working remotely are expected to maintain...
3. [Score: 0.7823] [EmployeeHandbook.md (Chunk 12/18)] ...equipment and internet connectivity requirements...
```

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