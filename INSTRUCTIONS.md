# Engineering Fundamentals of Retrieval Augmented Generation (RAG)
In the era of large language models (LLMs), the primary challenge for AI engineering has shifted from training models to effectively managing their context. RAG has emerged as the dominant architectural solution for enterprise AI, as it directly addresses the critical issues of hallucination and knowledge obsolescence (the LLM's knowledge being out-of-date).

RAG works by allowing an LLM to retrieve facts from an external, up-to-date knowledge source—such as a proprietary document database or a live web search—and use those retrieved documents to "ground" its answer. While the concept of retrieving a document and pasting it into a prompt seems simple, the process requires complex engineering trade-offs to build robust, production-ready systems.

This unit of study is designed to move you past viewing LLMs as "magic" APIs. Instead, you will transition into an architect of deterministic systems, exploring the first principles of RAG by dissecting its core pipeline into constituent engineering challenges: Chunking, Vectorization, Storage, Similarity, and Agentic Routing.

[Intro to RAG for AI (Retrieval Augmented Generation)](https://www.youtube.com/watch?v=Y08Nn23o_mY)

## Do Lab 1 in LAB1.md

## Storing data for retrieval

Vector databases are specialized storage systems designed to store, index, and query high-dimensional numerical vectors (embeddings) that represent the semantic meaning of text, images, or other data. Unlike traditional databases that search for exact keyword matches, vector databases enable similarity searches based on meaning and context.

Why Vector Databases Matter:

When you convert text into embeddings using models like OpenAI's text-embedding-ada-002 or similar, each piece of text becomes a vector of hundreds or thousands of dimensions. These vectors capture semantic relationships—so "king" and "monarch" will have similar vectors even though they share no letters. Vector databases make it possible to:

Find Similar Content - Retrieve documents semantically related to a query, even if they use completely different wording
Scale Efficiently - Handle millions of vectors with fast retrieval using specialized indexing algorithms (HNSW, IVF, etc.)
Power RAG Systems - Serve as the knowledge base for Retrieval Augmented Generation, providing relevant context to LLMs
Enable Semantic Search - Go beyond keyword matching to understand user intent and meaning
Common vector database solutions include:

Chroma - Lightweight, embeddable, open-source (great for development and small projects)
Pinecone - Managed cloud service with high performance and scalability
Weaviate - Open-source with GraphQL API and hybrid search capabilities
Qdrant - Rust-based, high-performance with advanced filtering
FAISS - Facebook's similarity search library (not a full database, but widely used)
Vector databases typically store both the original text (or metadata) and its corresponding embedding vector, enabling you to perform similarity searches and retrieve the actual content users need.

[What is a Vector Database?](https://www.youtube.com/watch?v=gl1r1XV0SLw)

## Do Lab 2 in LAB2.md

## Handling large documents by chunking

Chunking is the process of dividing large text documents into smaller, manageable pieces (chunks) before processing them with AI models or storing them in vector databases. This technique is essential because:

1. Token Limitations - Language models have maximum context windows (e.g., 4K, 8K, or 128K tokens). Large documents must be split to fit within these limits.
2. Semantic Precision - Smaller chunks enable more accurate semantic search and retrieval. When you query a vector database, you want to retrieve the most relevant paragraph or section, not an entire 100-page document.
3. Embedding Quality - Embedding models work best on coherent, focused text segments. A chunk containing a single concept or topic produces more meaningful vector representations than one mixing multiple unrelated ideas.
4. Cost & Performance - Processing smaller chunks reduces API costs and improves response times. You only retrieve and send relevant portions to the LLM instead of entire documents.
5. Contextual Relevance - In RAG (Retrieval Augmented Generation) systems, chunks allow you to inject precisely the information needed to answer a question, maximizing the utility of limited context windows.

Common chunking strategies include fixed-size splitting (by character or token count), recursive splitting (respecting document structure like paragraphs and sentences), and semantic chunking (grouping related content together).

[Chunking Strategies Explained](https://www.youtube.com/watch?v=ZTOtxiWb2bE)

## Do lab 3 in LAB3.md

## Do lab 4 in LAB4.md

Turn in a link to your repository