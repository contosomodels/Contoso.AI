# Contoso.AI.TextEmbedder

Base interface library for text embedding models in the Contoso.AI family. This package provides the `ITextEmbedder` interface and common types used by all text embedding model implementations.

## Overview

This package contains:
- `ITextEmbedder` - Interface that all embedding models implement
- `Embedding` - Class representing an embedding vector with utility methods like cosine similarity

## Usage

This is a base interface package. To use text embeddings, install a specific model implementation:

```bash
dotnet add package Contoso.AI.TextEmbedder.MiniLML6
```

## For Model Implementers

If you're creating a new text embedding model implementation, implement the `ITextEmbedder` interface:

```csharp
public class MyEmbedder : ITextEmbedder
{
    public Embedding[] GenerateEmbeddings(params string[] texts)
    {
        // Implementation
    }

    public Task<Embedding[]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        // Implementation
    }

    public void Dispose()
    {
        // Cleanup
    }
}
```

## License

MIT
