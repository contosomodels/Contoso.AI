namespace Contoso.AI;

/// <summary>
/// Represents a segmentation mask that identifies foreground and background regions in an image.
/// The mask uses RGBA format where:
/// - Background pixels: RGBA(255, 255, 255, 255) - white with full alpha
/// - Foreground pixels: RGBA(0, 0, 0, 0) - transparent
/// </summary>
public class SegmentationMask
{
    /// <summary>
    /// Gets or sets the width of the mask in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the mask in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the raw mask data in RGBA format (4 bytes per pixel).
    /// Background pixels have alpha = 255, foreground pixels have alpha = 0.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets whether the mask contains any foreground pixels.
    /// </summary>
    public bool HasForeground
    {
        get
        {
            // Check if any pixel has alpha = 0 (foreground)
            for (int i = 3; i < Data.Length; i += 4)
            {
                if (Data[i] == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Determines whether the pixel at the specified coordinates is foreground.
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <returns>True if the pixel is foreground; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when x or y is outside the bounds of the mask.
    /// </exception>
    public bool IsForeground(int x, int y)
    {
        if (x < 0 || x >= Width)
            throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {Width - 1}");
        if (y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException(nameof(y), $"y must be between 0 and {Height - 1}");

        int index = (y * Width + x) * 4 + 3; // Alpha channel
        return Data[index] == 0;
    }

    /// <summary>
    /// Determines whether the pixel at the specified coordinates is background.
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <returns>True if the pixel is background; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when x or y is outside the bounds of the mask.
    /// </exception>
    public bool IsBackground(int x, int y)
    {
        return !IsForeground(x, y);
    }

    /// <summary>
    /// Gets the foreground probability for a pixel (0 = background, 1 = foreground).
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <returns>1.0 if foreground, 0.0 if background.</returns>
    public float GetForegroundProbability(int x, int y)
    {
        return IsForeground(x, y) ? 1.0f : 0.0f;
    }

    /// <summary>
    /// Counts the number of foreground pixels in the mask.
    /// </summary>
    /// <returns>The number of foreground pixels.</returns>
    public int CountForegroundPixels()
    {
        int count = 0;
        for (int i = 3; i < Data.Length; i += 4)
        {
            if (Data[i] == 0)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Counts the number of background pixels in the mask.
    /// </summary>
    /// <returns>The number of background pixels.</returns>
    public int CountBackgroundPixels()
    {
        return (Width * Height) - CountForegroundPixels();
    }

    /// <summary>
    /// Gets the percentage of the image that is foreground.
    /// </summary>
    /// <returns>A value between 0.0 and 1.0 representing the foreground percentage.</returns>
    public float GetForegroundPercentage()
    {
        int total = Width * Height;
        if (total == 0) return 0;
        return (float)CountForegroundPixels() / total;
    }
}
