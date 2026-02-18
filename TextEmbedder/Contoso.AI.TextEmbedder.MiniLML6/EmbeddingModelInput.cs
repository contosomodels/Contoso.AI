namespace Contoso.AI.TextEmbedder.MiniLML6;

internal class EmbeddingModelInput
{
    public required long[] InputIds { get; init; }

    public required long[] AttentionMask { get; init; }

    public required long[] TokenTypeIds { get; init; }
}
