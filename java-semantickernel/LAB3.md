
## Lab 3: Loading Real Documents - Discovering the Limits

In Labs 1 and 2, you worked with short sentences. But in the real world, you need to search through actual documents like employee handbooks, product manuals, or knowledge bases. In this lab, you'll attempt to load real markdown files into your vector database and discover firsthand why **chunking** is essential.

**📚 Key Concepts:**
- [Token Limits in Embedding Models](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#embeddings) - Understanding model limitations
- [Vector Store Data Ingestion](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/how-to/vector-store-data-ingestion) - Best practices for loading data

### The Challenge: From Sentences to Documents

Your workspace contains two markdown files:
- **HealthInsuranceBrochure.md** - A moderate-sized document (~123 lines)
- **EmployeeHandbook.md** - A large document (~3,267 lines)

You'll start by trying to store these documents as-is (without chunking) and observe what happens when you hit the limits of embedding models.

---

### Step 1: Update Your Data Model for Documents

First, you need to modify your record structure to handle documents instead of short sentences.

**Prompt 1: Update the Data Model**
```
I need to modify the SentenceRecord class to work with documents instead of sentences.
Please rename it to DocumentRecord and update it to include:
- id (String) with @VectorStoreRecordKey
- fileName (String) with @VectorStoreRecordData - to track which file this came from
- content (String) with @VectorStoreRecordData - the actual document text
- embedding (List<Float>) with @VectorStoreRecordVector and dimension 1536
- createdAt (OffsetDateTime) with @VectorStoreRecordData

Include a constructor with all fields and getter methods.
Also update any references to SentenceRecord throughout the code to use DocumentRecord instead.
```

**What to Look For:** Your data model now stores full documents with their filenames, making it easier to track where content came from.

---

### Step 2: Clear Previous Data and Prepare for Documents

Before loading documents, you need to clean up the sentence data from Lab 2.

**Prompt 2: Remove the Sentence Loading Code**
```
Comment out or remove all the code that:
- Creates the sentences list
- Loops through sentences generating embeddings
- Stores sentence records in the vector database

Keep the vector store initialization code, but we'll be adding different data loading logic.
```

**What to Look For:** Your App.java should still initialize the kernel, embedding service, and vector store collection, but no longer populate it with sentences.

---

### Step 3: Load the Health Insurance Brochure

Now you'll create a function to read a markdown file and store it in the vector database.

**📚 Reference:** [File I/O in Java](https://docs.oracle.com/en/java/javase/17/docs/api/java.base/java/nio/file/Files.html) - Learn about reading files in Java.

**Prompt 3: Create File Loading Function**
```
Create a static method called loadDocumentAsync that:
- Takes parameters: the embedding service, the vector store collection, and a file path (String)
- Reads all text from the file using Files.readString(Paths.get(filePath))
- Generates an embedding for the entire document content
- Creates a new DocumentRecord with:
  - id: A UUID using UUID.randomUUID().toString()
  - fileName: The file name from the path (use Paths.get(filePath).getFileName().toString())
  - content: The full document text
  - embedding: The generated embedding
  - createdAt: Current UTC time using OffsetDateTime.now(ZoneOffset.UTC)
- Upserts the record to the collection using upsertAsync
- Prints a success message with the filename and content length

Add error handling for file not found and other exceptions using try-catch.
```

**What to Look For:** This method encapsulates the logic for loading a single document into the vector database.

---

### Step 4: Load the First Document

Let's try loading the health insurance brochure first.

**Prompt 4: Load HealthInsuranceBrochure.md**
```
After the vector store collection is created, call the loadDocumentAsync method to load the file "HealthInsuranceBrochure.md" from the workspace root directory.
Display a header "=== Loading Documents into Vector Database ===" before loading.
Print the filename and a confirmation when it loads successfully.
```

**Test Point**: Run the application using `mvn clean compile exec:java` or the "maven build" task. 

**Expected Output:**
```
🤖 C# Semantic Kernel Agent Starting...

=== Loading Documents into Vector Database ===

Loading HealthInsuranceBrochure.md...
✅ Successfully loaded HealthInsuranceBrochure.md (... characters)
```

**What You Should Observe:**
- The brochure loads successfully
- No errors occur
- The embedding is generated without issues

This works because the health insurance brochure is small enough to fit within the embedding model's token limits.

---

### Step 5: Try Loading the Employee Handbook

Now let's see what happens when you try to load a much larger document.

**Prompt 5: Add Loading for EmployeeHandbook.md**
```
After loading the HealthInsuranceBrochure.md, add another call to loadDocumentAsync to load "EmployeeHandbook.md" from the workspace root directory.
```

**Test Point**: Run the application again using `mvn clean compile exec:java`.

**What Will Happen:**
You'll encounter an error! The embedding model has a maximum token limit (typically around 8,191 tokens for text-embedding-3-small), and the Employee Handbook is too large to process in one go.

**Expected Error:**
```
❌ Error loading EmployeeHandbook.md: 
This model's maximum context length is 8191 tokens. However, your messages resulted in 15234 tokens.
```

**Why This Happens:**
- The EmployeeHandbook.md contains over 3,000 lines of text
- When converted to tokens, it exceeds the embedding model's 8,191 token limit
- The embedding API rejects the request
- Your document cannot be stored as-is

---

### Step 6: Observe and Document the Problem

Before moving to the solution, let's understand what just happened.

**Prompt 6: Add Error Analysis**
```
In the loadDocumentAsync method's catch block, enhance the error handling to:
- Check if the exception message contains "maximum context length" or "token"
- If so, display a special message explaining:
  "⚠️ This document is too large to embed as a single chunk."
  "Token limit exceeded. The embedding model can only process up to 8,191 tokens at once."
  "Solution: The document needs to be split into smaller chunks."
- Otherwise, display the regular error message using e.getMessage()
```

**Test Again**: Run `mvn clean compile exec:java` one more time.

**Expected Output:**
```
🤖 Java Semantic Kernel Agent Starting...

=== Loading Documents into Vector Database ===

Loading HealthInsuranceBrochure.md...
✅ Successfully loaded HealthInsuranceBrochure.md (2,456 characters)

Loading EmployeeHandbook.md...
❌ Error loading EmployeeHandbook.md
⚠️ This document is too large to embed as a single chunk.
Token limit exceeded. The embedding model can only process up to 8,191 tokens at once.
Solution: The document needs to be split into smaller chunks.
```

---

### Understanding the Problem

**📚 Learn More:** [Why Chunking Matters in RAG](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/how-to/vector-store-data-ingestion) - Understanding document chunking strategies.

**What You've Discovered:**

1. **Token Limits Are Real** - Embedding models cannot process unlimited text. They have hard limits (typically 512 to 8,191 tokens depending on the model).

2. **Documents Vary in Size** - Some documents fit within limits (HealthInsuranceBrochure.md), while others don't (EmployeeHandbook.md).

3. **One-Size-Fits-All Doesn't Work** - You can't just load every document as a single vector. Large documents must be broken down.

4. **Chunking is Essential** - To work with real-world documents in RAG systems, you need a strategy to divide them into processable pieces.

**The Core Problem:**
```
Full Document (15,234 tokens) → ❌ Too large for embedding model
Full Document Split into Chunks (10 chunks × ~1,500 tokens) → ✅ Each chunk can be embedded
```

---

### Discussion Questions

Before moving to the chunking solution, consider:

1. **Why can't we just use a larger embedding model?** Even with larger context windows, there are performance and cost trade-offs. Plus, semantic search works better with focused chunks.

2. **What information might we lose when splitting a document?** Context that spans across chunk boundaries could be harder to retrieve.

3. **How would you decide where to split a document?** By paragraphs? By headings? By a fixed character count?

4. **What metadata should we preserve?** When you chunk a document, what information should each chunk retain about its source?

---

### What's Next?

You've successfully identified the problem: **large documents exceed embedding model token limits**. 

In the next part of this lab, you'll implement **document chunking** - a strategy to intelligently split the EmployeeHandbook.md into smaller, semantically meaningful pieces that can each be embedded and searched independently.

**Save your work** - you'll build on this code in the next section.

---
