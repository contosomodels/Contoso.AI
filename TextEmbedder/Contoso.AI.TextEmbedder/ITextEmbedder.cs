namespace Contoso.AI;

/// <summary>
/// Interface for text embedding generation models.
/// </summary>
public interface ITextEmbedder : IDisposable
{
    /// <summary>
    /// Generates embeddings for the provided texts.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <returns>An array of embeddings, one for each input text.</returns>
    Embedding[] GenerateEmbeddings(params string[] texts);

    /// <summary>
    /// Generates embeddings for the provided texts asynchronously.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing an array of embeddings, one for each input text.</returns>
    Task<Embedding[]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
