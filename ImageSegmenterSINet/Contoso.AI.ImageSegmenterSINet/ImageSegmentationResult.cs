using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

    /// <summary>
    /// Creates a visual overlay of the segmentation mask on the original image.
    /// </summary>
    /// <param name="originalImage">The original image.</param>
    /// <param name="overlayColor">The color to use for the background overlay. Default is semi-transparent red.</param>
    /// <returns>A new bitmap with the segmentation mask overlaid.</returns>
    public Bitmap CreateMaskOverlay(Bitmap originalImage, Color? overlayColor = null)
    {
        ArgumentNullException.ThrowIfNull(originalImage);

        var overlay = new Bitmap(originalImage);
        var color = overlayColor ?? Color.FromArgb(100, 255, 0, 0);

        using var g = Graphics.FromImage(overlay);
        using var brush = new SolidBrush(color);

        var mask = Mask;
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                int index = (y * mask.Width + x) * 4;
                // If alpha channel indicates background (opaque = background in our mask)
                if (mask.Data[index + 3] > 128)
                {
                    g.FillRectangle(brush, x, y, 1, 1);
                }
            }
        }

        return overlay;
    }

    /// <summary>
    /// Extracts the foreground from an image, making the background transparent.
    /// </summary>
    /// <param name="originalImage">The original image.</param>
    /// <returns>A new bitmap with transparent background.</returns>
    public Bitmap ExtractForeground(Bitmap originalImage)
    {
        ArgumentNullException.ThrowIfNull(originalImage);

        var output = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
        var mask = Mask;

        var srcData = originalImage.LockBits(
            new Rectangle(0, 0, originalImage.Width, originalImage.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var dstData = output.LockBits(
            new Rectangle(0, 0, output.Width, output.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            int stride = srcData.Stride;
            int bytes = Math.Abs(stride) * originalImage.Height;
            byte[] srcPixels = new byte[bytes];
            byte[] dstPixels = new byte[bytes];

            Marshal.Copy(srcData.Scan0, srcPixels, 0, bytes);

            for (int y = 0; y < mask.Height; y++)
            {
                for (int x = 0; x < mask.Width; x++)
                {
                    int pixelIndex = y * stride + x * 4;
                    int maskIndex = (y * mask.Width + x) * 4;

                    // If background (mask alpha > 128), make transparent
                    if (mask.Data[maskIndex + 3] > 128)
                    {
                        dstPixels[pixelIndex] = 0;     // B
                        dstPixels[pixelIndex + 1] = 0; // G
                        dstPixels[pixelIndex + 2] = 0; // R
                        dstPixels[pixelIndex + 3] = 0; // A
                    }
                    else
                    {
                        // Keep foreground pixels
                        dstPixels[pixelIndex] = srcPixels[pixelIndex];         // B
                        dstPixels[pixelIndex + 1] = srcPixels[pixelIndex + 1]; // G
                        dstPixels[pixelIndex + 2] = srcPixels[pixelIndex + 2]; // R
                        dstPixels[pixelIndex + 3] = 255;                       // A (fully opaque)
                    }
                }
            }

            Marshal.Copy(dstPixels, 0, dstData.Scan0, bytes);
        }
        finally
        {
            originalImage.UnlockBits(srcData);
            output.UnlockBits(dstData);
        }

        return output;
    }
}
