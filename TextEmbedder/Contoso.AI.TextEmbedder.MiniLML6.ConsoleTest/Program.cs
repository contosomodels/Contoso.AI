using Contoso.AI;

Console.WriteLine("=== Contoso.AI Text Embedder (MiniLM-L6-v2) Test ===");
Console.WriteLine();

// Check if the feature is ready
Console.WriteLine("Checking feature readiness...");
var readyState = TextEmbedderMiniLML6.GetReadyState();
Console.WriteLine($"Ready State: {readyState}");

if (readyState != AIFeatureReadyState.Ready)
{
    Console.WriteLine("Feature not ready. Ensuring dependencies are available...");
    var readyResult = await TextEmbedderMiniLML6.EnsureReadyAsync();
    
    if (readyResult.Status != AIFeatureReadyResultState.Success)
    {
        Console.WriteLine($"Failed to ensure ready: {readyResult.ExtendedError?.Message}");
        return;
    }
    
    Console.WriteLine("Feature is now ready!");
}

Console.WriteLine();

// Create embedder instance
Console.WriteLine("Creating text embedder...");
using var embedder = await TextEmbedderMiniLML6.CreateAsync();
Console.WriteLine("Text embedder created successfully!");
Console.WriteLine();

// Test with sample texts
var sampleTexts = new[]
{
    "The quick brown fox jumps over the lazy dog",
    "A fast auburn fox leaps above an idle canine",
    "Python is a popular programming language",
    "Cats are wonderful pets",
    "Machine learning is transforming technology"
};

Console.WriteLine("Generating embeddings for sample texts:");
foreach (var text in sampleTexts)
{
    Console.WriteLine($"  - \"{text}\"");
}
Console.WriteLine();

var embeddings = embedder.GenerateEmbeddings(sampleTexts);

Console.WriteLine($"Generated {embeddings.Length} embeddings");
Console.WriteLine($"Embedding dimensions: {embeddings[0].Dimensions}");
Console.WriteLine();

// Calculate and display similarity matrix
Console.WriteLine("Cosine Similarity Matrix:");
Console.WriteLine("(Values close to 1.0 indicate high similarity)");
Console.WriteLine();

for (int i = 0; i < embeddings.Length; i++)
{
    Console.Write($"Text {i + 1}: ");
    for (int j = 0; j < embeddings.Length; j++)
    {
        var similarity = embeddings[i].CosineSimilarity(embeddings[j]);
        Console.Write($"{similarity:F3}  ");
    }
    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("Analysis:");
Console.WriteLine("- Texts 1 and 2 should have high similarity (both about foxes)");
Console.WriteLine("- Text 3, 4, and 5 should be less similar to 1 and 2");
Console.WriteLine();

// Demonstrate semantic search
Console.WriteLine("=== Semantic Search Demo ===");
var query = "Domestic animals";
Console.WriteLine($"Query: \"{query}\"");
Console.WriteLine();

var queryEmbedding = embedder.GenerateEmbeddings(query)[0];

Console.WriteLine("Similarity scores:");
for (int i = 0; i < sampleTexts.Length; i++)
{
    var similarity = queryEmbedding.CosineSimilarity(embeddings[i]);
    Console.WriteLine($"  {similarity:F4} - {sampleTexts[i]}");
}

Console.WriteLine();
Console.WriteLine("Test completed successfully!");
