namespace Contoso.AI;

/// <summary>
/// Represents a text embedding vector.
/// </summary>
public class Embedding
{
    /// <summary>
    /// Gets the embedding vector as an array of floats.
    /// </summary>
    public float[] Vector { get; }

    /// <summary>
    /// Gets the dimensionality of the embedding.
    /// </summary>
    public int Dimensions => Vector.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="Embedding"/> class.
    /// </summary>
    /// <param name="vector">The embedding vector.</param>
    public Embedding(float[] vector)
    {
        Vector = vector ?? throw new ArgumentNullException(nameof(vector));
    }

    /// <summary>
    /// Calculates the cosine similarity between this embedding and another.
    /// </summary>
    /// <param name="other">The other embedding to compare with.</param>
    /// <returns>The cosine similarity (between -1 and 1, where 1 is most similar).</returns>
    public float CosineSimilarity(Embedding other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (Vector.Length != other.Vector.Length)
            throw new ArgumentException("Embeddings must have the same dimensionality", nameof(other));

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < Vector.Length; i++)
        {
            dotProduct += Vector[i] * other.Vector[i];
            normA += Vector[i] * Vector[i];
            normB += other.Vector[i] * other.Vector[i];
        }

        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
