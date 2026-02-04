namespace Contoso.AI;

/// <summary>
/// Represents the result of image segmentation, containing the segmentation mask
/// and metadata about the original image.
/// </summary>
public class ImageSegmentationResult
{
    /// <summary>
    /// Gets or sets the segmentation mask that separates foreground from background.
    /// </summary>
    public SegmentationMask Mask { get; set; } = new();

    /// <summary>
    /// Gets or sets the original width of the input image.
    /// </summary>
    public int OriginalWidth { get; set; }

    /// <summary>
    /// Gets or sets the original height of the input image.
    /// </summary>
    public int OriginalHeight { get; set; }

    /// <summary>
    /// Gets whether the segmentation detected any foreground in the image.
    /// </summary>
    public bool HasForeground => Mask.HasForeground;
}
