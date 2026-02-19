# Contoso.AI.TextEmbedder.MiniLML6

[![NuGet](https://img.shields.io/nuget/v/Contoso.AI.TextEmbedder.MiniLML6.svg)](https://www.nuget.org/packages/Contoso.AI.TextEmbedder.MiniLML6/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

AI-powered text embedding library using the **all-MiniLM-L6-v2** ONNX model from sentence-transformers. Converts text into 384-dimensional embeddings for semantic search, similarity tasks, clustering, and more.

## ✨ Features

- **384-Dimensional Embeddings** - Rich semantic representations for text
- **Automatic Model Download** - Model downloads automatically at build time
- **GPU Acceleration** - Leverages DirectML for faster inference when available
- **Easy-to-Use API** - Simple async factory pattern with `CreateAsync()`
- **Cosine Similarity** - Built-in similarity calculation between embeddings
- **Semantic Search** - Find semantically similar texts
- **NuGet Package** - Easy integration into any .NET project

## 📋 Requirements

- **Windows 10** SDK 19041 or later
- **.NET 8.0** or later
- **GPU** (optional) - DirectML-compatible GPU for hardware acceleration
- Falls back to CPU execution if GPU is not available

## 🚀 Quick Start

### Installation

```bash
dotnet add package Contoso.AI.TextEmbedder.MiniLML6
```

Or via Package Manager Console:

```powershell
Install-Package Contoso.AI.TextEmbedder.MiniLML6
```

### Basic Usage

```csharp
using Contoso.AI;

// Check if the feature is ready
var readyState = TextEmbedderMiniLML6.GetReadyState();

if (readyState != AIFeatureReadyState.Ready)
{
    // Prepare the feature (downloads model if needed)
    var readyResult = await TextEmbedderMiniLML6.EnsureReadyAsync();
    if (readyResult.Status != AIFeatureReadyResultState.Success)
    {
        Console.WriteLine($"Failed to initialize: {readyResult.ExtendedError?.Message}");
        return;
    }
}

// Create embedder instance (returns ITextEmbedder interface)
using ITextEmbedder embedder = await TextEmbedderMiniLML6.CreateAsync();

// Generate embeddings
var texts = new[]
{
    "The quick brown fox jumps over the lazy dog",
    "Machine learning is transforming technology"
};

var embeddings = embedder.GenerateEmbeddings(texts);

// Calculate similarity
var similarity = embeddings[0].CosineSimilarity(embeddings[1]);
Console.WriteLine($"Similarity: {similarity:F3}");
```

### Semantic Search Example

```csharp
using Contoso.AI;

using ITextEmbedder embedder = await TextEmbedderMiniLML6.CreateAsync();

// Index some documents
var documents = new[]
{
    "Python is a programming language",
    "Cats make great pets",
    "Machine learning uses neural networks",
    "Dogs are loyal companions"
};

var docEmbeddings = embedder.GenerateEmbeddings(documents);

// Search with a query
var query = "Domestic animals";
var queryEmbedding = embedder.GenerateEmbeddings(query)[0];

// Find most similar documents
var results = docEmbeddings
    .Select((emb, idx) => new { 
        Document = documents[idx], 
        Score = queryEmbedding.CosineSimilarity(emb) 
    })
    .OrderByDescending(x => x.Score)
    .ToList();

foreach (var result in results)
{
    Console.WriteLine($"{result.Score:F3} - {result.Document}");
}
```

## 📖 API Reference

### TextEmbedderMiniLML6 Class

| Method | Description |
|--------|-------------|
| `GetReadyState()` | Static. Returns `AIFeatureReadyState` indicating if the feature can be used |
| `EnsureReadyAsync()` | Static. Checks for required dependencies |
| `CreateAsync()` | Static factory. Creates and initializes a new `ITextEmbedder` instance |

### ITextEmbedder Interface

| Method | Description |
|--------|-------------|
| `GenerateEmbeddings(params string[])` | Synchronously generates embeddings for texts |
| `GenerateEmbeddingsAsync(IEnumerable<string>, CancellationToken)` | Asynchronously generates embeddings for texts |

### Embedding Class

| Property/Method | Description |
|-----------------|-------------|
| `Vector` | Gets the embedding as a float array |
| `Dimensions` | Gets the dimensionality (384 for MiniLM-L6-v2) |
| `CosineSimilarity(Embedding)` | Calculates cosine similarity with another embedding (returns -1 to 1) |

## 🏗️ Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/contoso/Contoso.AI.git
cd Contoso.AI/TextEmbedder

# Restore and build (model downloads automatically)
dotnet restore
dotnet build

# Run the console test
dotnet run --project Contoso.AI.TextEmbedder.MiniLML6.ConsoleTest
```

### Model Information

This project uses the **all-MiniLM-L6-v2** model from sentence-transformers:

- **Source**: [sentence-transformers/all-MiniLM-L6-v2 on Hugging Face](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2)
- **Commit**: d83dd3760b5bfe921f2fe125446b17bf0b7eda8c
- **Format**: ONNX
- **Output Dimensions**: 384
- **License**: Apache 2.0

The model is automatically downloaded during the first build and cached in the `obj/Models` directory.

## 🧪 Use Cases

- **Semantic Search** - Find documents similar to a query
- **Text Clustering** - Group similar texts together
- **Duplicate Detection** - Identify similar or duplicate content
- **Recommendation Systems** - Recommend similar items based on text descriptions
- **Question Answering** - Find answers similar to questions
- **Content Classification** - Classify text by similarity to categories

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [sentence-transformers](https://www.sbert.net/) for the all-MiniLM-L6-v2 model
- [ONNX Runtime](https://onnxruntime.ai/) for the inference engine
- [Microsoft AI Dev Gallery](https://github.com/microsoft/ai-dev-gallery) for embedding generation code samples

---

**Made with ❤️ by Contoso**
