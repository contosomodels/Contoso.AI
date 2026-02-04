using Contoso.AI;
using System.Drawing;
using System.Drawing.Imaging;

Console.WriteLine("=== Contoso.AI.ImageSegmenterSINet Console Test ===");
Console.WriteLine("Using SINet (Salient Image Network) for foreground/background segmentation");
Console.WriteLine();

const string imagePath = "Assets/SampleImage.png";
const string outputMaskPath = "output_mask.png";
const string outputOverlayPath = "output_overlay.png";
const string outputForegroundPath = "output_foreground.png";

if (!File.Exists(imagePath))
{
    Console.WriteLine($"ERROR: Sample image not found at: {imagePath}");
    Console.WriteLine();
    Console.WriteLine("Please add a sample image named 'SampleImage.png' to the Assets folder.");
    Console.WriteLine("You can use any image with a clear foreground subject (person, object, etc.)");
    return;
}

Console.WriteLine($"Loading image: {imagePath}");
Console.WriteLine();

try
{
    // Check if the feature is ready
    Console.WriteLine("Checking if ImageSegmenterSINet is ready...");
    var readyState = ImageSegmenterSINet.GetReadyState();

    if (readyState != AIFeatureReadyState.Ready)
    {
        Console.WriteLine($"ImageSegmenterSINet is not ready. State: {readyState}");
        Console.WriteLine("Attempting to prepare the feature...");

        var readyResult = await ImageSegmenterSINet.EnsureReadyAsync();

        if (readyResult.Status != AIFeatureReadyResultState.Success)
        {
            Console.WriteLine($"ERROR: Failed to prepare: {readyResult.ExtendedError?.Message}");
            return;
        }

        Console.WriteLine("ImageSegmenterSINet is now ready!");
    }
    else
    {
        Console.WriteLine("ImageSegmenterSINet is ready!");
    }

    Console.WriteLine();

    // Create the segmenter instance
    Console.WriteLine("Creating ImageSegmenterSINet instance...");
    using var segmenter = await ImageSegmenterSINet.CreateAsync();
    Console.WriteLine("ImageSegmenterSINet created successfully!");
    Console.WriteLine();

    // Load and process the image
    Console.WriteLine("Loading and analyzing image...");
    using var bitmap = new Bitmap(imagePath);
    Console.WriteLine($"Image size: {bitmap.Width}x{bitmap.Height}");
    Console.WriteLine();

    // Run segmentation
    Console.WriteLine("Running image segmentation...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var result = segmenter.SegmentImage(bitmap);
    stopwatch.Stop();

    // Output results
    Console.WriteLine();
    Console.WriteLine("=== RESULTS ===");
    Console.WriteLine($"Segmentation completed in: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"Image dimensions: {result.OriginalWidth}x{result.OriginalHeight}");
    Console.WriteLine($"Has foreground: {result.HasForeground}");
    Console.WriteLine($"Foreground pixels: {result.Mask.CountForegroundPixels():N0}");
    Console.WriteLine($"Background pixels: {result.Mask.CountBackgroundPixels():N0}");
    Console.WriteLine($"Foreground percentage: {result.Mask.GetForegroundPercentage():P2}");
    Console.WriteLine();

    // Create and save mask overlay
    Console.WriteLine("Creating visualization outputs...");
    
    using var overlay = result.CreateMaskOverlay(bitmap);
    overlay.Save(outputOverlayPath, ImageFormat.Png);
    Console.WriteLine($"  Saved mask overlay to: {outputOverlayPath}");

    // Create and save foreground extraction
    using var foreground = result.ExtractForeground(bitmap);
    foreground.Save(outputForegroundPath, ImageFormat.Png);
    Console.WriteLine($"  Saved extracted foreground to: {outputForegroundPath}");

    // Create and save raw mask
    using var maskBitmap = new Bitmap(result.Mask.Width, result.Mask.Height, PixelFormat.Format32bppArgb);
    var maskData = maskBitmap.LockBits(
        new Rectangle(0, 0, maskBitmap.Width, maskBitmap.Height),
        ImageLockMode.WriteOnly,
        PixelFormat.Format32bppArgb);
    System.Runtime.InteropServices.Marshal.Copy(result.Mask.Data, 0, maskData.Scan0, result.Mask.Data.Length);
    maskBitmap.UnlockBits(maskData);
    maskBitmap.Save(outputMaskPath, ImageFormat.Png);
    Console.WriteLine($"  Saved raw mask to: {outputMaskPath}");

    Console.WriteLine();
    Console.WriteLine("=== TEST COMPLETE ===");
    Console.WriteLine();
    Console.WriteLine("Output files created:");
    Console.WriteLine($"  - {outputMaskPath} (raw segmentation mask)");
    Console.WriteLine($"  - {outputOverlayPath} (original with background highlighted in red)");
    Console.WriteLine($"  - {outputForegroundPath} (foreground with transparent background)");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
