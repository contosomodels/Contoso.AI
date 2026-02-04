using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Windows.AI.MachineLearning;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Contoso.AI;

/// <summary>
/// Provides image segmentation capabilities using SINet (Salient Image Network) for 
/// foreground/background separation on NPU hardware acceleration.
/// </summary>
public sealed class ImageSegmenterSINet : IDisposable
{
    private readonly OrtEnv _env;
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _modelInputWidth;
    private readonly int _modelInputHeight;
    private readonly bool _isQuantized;
    private bool _disposed;

    private const string FloatModelPath = "Models/SINet/float/model.onnx";
    private const string QuantizedModelPath = "Models/SINet/quantized/model.onnx";

    /// <summary>
    /// Private constructor - use <see cref="CreateAsync"/> factory method.
    /// </summary>
    private ImageSegmenterSINet(OrtEnv env, InferenceSession session, string inputName, int modelInputWidth, int modelInputHeight, bool isQuantized)
    {
        _env = env;
        _session = session;
        _inputName = inputName;
        _modelInputWidth = modelInputWidth;
        _modelInputHeight = modelInputHeight;
        _isQuantized = isQuantized;
    }

    /// <summary>
    /// Gets the ready state of the image segmenter feature.
    /// </summary>
    /// <returns>The ready state indicating if the feature can be used.</returns>
    public static AIFeatureReadyState GetReadyState()
    {
        try
        {
            // Check if model files exist
            if (!File.Exists(FloatModelPath) && !File.Exists(QuantizedModelPath))
            {
                Debug.WriteLine($"[ImageSegmenterSINet] Model files not found");
                return AIFeatureReadyState.NotReady;
            }

            // Check if Windows ML EP catalog is available
            var catalog = ExecutionProviderCatalog.GetDefault();
            var qnnProvider = catalog.FindAllProviders()
                .FirstOrDefault(i => i.Name == "QNNExecutionProvider");

            if (qnnProvider == null)
            {
                Debug.WriteLine("[ImageSegmenterSINet] QNN Execution Provider not found");
                return AIFeatureReadyState.NotReady;
            }

            if (qnnProvider.ReadyState == ExecutionProviderReadyState.NotPresent)
            {
                Debug.WriteLine("[ImageSegmenterSINet] QNN Execution Provider not present, needs download");
                return AIFeatureReadyState.NotReady;
            }

            return AIFeatureReadyState.Ready;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageSegmenterSINet] Error checking ready state: {ex.Message}");
            return AIFeatureReadyState.NotReady;
        }
    }

    /// <summary>
    /// Ensures the image segmenter feature is ready by downloading necessary dependencies.
    /// </summary>
    /// <returns>A task containing the preparation result.</returns>
    public static async Task<AIFeatureReadyResult> EnsureReadyAsync()
    {
        try
        {
            // Check if model files exist
            if (!File.Exists(FloatModelPath) && !File.Exists(QuantizedModelPath))
            {
                throw new FileNotFoundException($"Model files not found: {FloatModelPath} or {QuantizedModelPath}");
            }

            // Get the Windows ML EP catalog
            var catalog = ExecutionProviderCatalog.GetDefault();

            // Get the QNN EP provider info
            var qnnProvider = catalog.FindAllProviders()
                .FirstOrDefault(i => i.Name == "QNNExecutionProvider");

            // If its ReadyState is NotPresent, download the EP
            if (qnnProvider != null &&
                qnnProvider.ReadyState == ExecutionProviderReadyState.NotPresent)
            {
                Debug.WriteLine("[ImageSegmenterSINet] Downloading QNN Execution Provider...");
                await qnnProvider.EnsureReadyAsync();
            }

            // Register all EPs with ONNX Runtime
            await catalog.RegisterCertifiedAsync();

            Debug.WriteLine("[ImageSegmenterSINet] Feature is ready");
            return AIFeatureReadyResult.Success();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageSegmenterSINet] Failed to ensure ready: {ex.Message}");
            return AIFeatureReadyResult.Failed(ex);
        }
    }

    /// <summary>
    /// Creates a new image segmenter instance with NPU acceleration.
    /// </summary>
    /// <returns>A task containing the initialized instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the model file is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when initialization fails.</exception>
    public static async Task<ImageSegmenterSINet> CreateAsync()
    {
        if (!File.Exists(FloatModelPath) && !File.Exists(QuantizedModelPath))
        {
            throw new FileNotFoundException($"Model files not found: {FloatModelPath} or {QuantizedModelPath}");
        }

        // Ensure dependencies are ready
        var readyResult = await EnsureReadyAsync();
        if (readyResult.Status != AIFeatureReadyResultState.Success)
        {
            throw readyResult.ExtendedError ??
                new InvalidOperationException("Failed to prepare feature");
        }

        // Get the ORT environment
        var env = OrtEnv.Instance();

        // Get the available EP devices
        var epDevices = env.GetEpDevices();

        // Check if QNN NPU is available
        var qnnEp = epDevices.FirstOrDefault(i =>
            i.EpName == "QNNExecutionProvider" &&
            i.HardwareDevice.Type == OrtHardwareDeviceType.NPU);

        // Select model and execution provider based on availability
        string modelPath;
        OrtEpDevice ep;
        bool isQuantized;

        if (qnnEp != null && File.Exists(QuantizedModelPath))
        {
            // Use quantized model with QNN NPU
            modelPath = QuantizedModelPath;
            ep = qnnEp;
            isQuantized = true;
            Debug.WriteLine("[ImageSegmenterSINet] Using quantized model with QNN NPU");
        }
        else if (File.Exists(FloatModelPath))
        {
            // Fall back to float model with CPU
            modelPath = FloatModelPath;
            ep = epDevices.First(i => i.EpName == "CPUExecutionProvider");
            isQuantized = false;
            Debug.WriteLine("[ImageSegmenterSINet] Using float model with CPU");
        }
        else
        {
            throw new FileNotFoundException("No suitable model found for available execution provider");
        }

        // Configure session options
        var sessionOptions = new SessionOptions();
        sessionOptions.AppendExecutionProvider(env, new[] { ep }, null);

        // Create the inference session
        var session = new InferenceSession(modelPath, sessionOptions);

        // Get input metadata
        var inputMeta = session.InputMetadata.First();
        var inputName = inputMeta.Key;
        var dimensions = inputMeta.Value.Dimensions;
        var modelInputHeight = dimensions[2];
        var modelInputWidth = dimensions[3];

        Debug.WriteLine($"[ImageSegmenterSINet] Created with model: {modelPath}");
        Debug.WriteLine($"[ImageSegmenterSINet] Model input size: {modelInputWidth}x{modelInputHeight}");
        Debug.WriteLine($"[ImageSegmenterSINet] Execution provider: {ep.EpName}");
        Debug.WriteLine($"[ImageSegmenterSINet] Model type: {(isQuantized ? "Quantized (UInt8)" : "Float (FP32)")}");

        return new ImageSegmenterSINet(env, session, inputName, modelInputWidth, modelInputHeight, isQuantized);
    }

    /// <summary>
    /// Segments the foreground from background in the provided image.
    /// </summary>
    /// <param name="bitmap">The input image to segment.</param>
    /// <returns>An <see cref="ImageSegmentationResult"/> containing the segmentation mask.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bitmap is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the segmenter has been disposed.</exception>
    public ImageSegmentationResult SegmentImage(Bitmap bitmap)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(bitmap);

        int originalWidth = bitmap.Width;
        int originalHeight = bitmap.Height;

        // Resize with padding to match model input dimensions
        using var resizedImage = ResizeWithPadding(bitmap, _modelInputWidth, _modelInputHeight);

        // Run inference based on model type
        IEnumerable<float> output;
        
        if (_isQuantized)
        {
            // Quantized model expects UInt8 input
            var inputDimensions = new int[] { 1, 3, _modelInputHeight, _modelInputWidth };
            Tensor<byte> input = new DenseTensor<byte>(inputDimensions);
            input = PreprocessBitmapQuantized(resizedImage, input);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, input)
            };

            using var results = _session.Run(inputs);
            
            // Quantized model output is also UInt8, need to dequantize to float
            var rawOutput = results[0].AsTensor<byte>();
            output = DequantizeOutput(rawOutput);
        }
        else
        {
            // Float model expects Float input
            var inputDimensions = new int[] { 1, 3, _modelInputHeight, _modelInputWidth };
            Tensor<float> input = new DenseTensor<float>(inputDimensions);
            input = PreprocessBitmapFloat(resizedImage, input);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, input)
            };

            using var results = _session.Run(inputs);
            output = results[0].AsEnumerable<float>();
        }

        // Generate foreground mask
        var maskData = GetForegroundMask(output, _modelInputWidth, _modelInputHeight, originalWidth, originalHeight);

        return new ImageSegmentationResult
        {
            Mask = new SegmentationMask
            {
                Width = originalWidth,
                Height = originalHeight,
                Data = maskData
            },
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight
        };
    }

    /// <summary>
    /// Creates a visual overlay of the segmentation mask on the original image.
    /// </summary>
    /// <param name="originalImage">The original image.</param>
    /// <param name="result">The segmentation result.</param>
    /// <param name="overlayColor">The color to use for the background overlay. Default is semi-transparent red.</param>
    /// <returns>A new bitmap with the segmentation mask overlaid.</returns>
    public static Bitmap CreateMaskOverlay(Bitmap originalImage, ImageSegmentationResult result, Color? overlayColor = null)
    {
        ArgumentNullException.ThrowIfNull(originalImage);
        ArgumentNullException.ThrowIfNull(result);

        var overlay = new Bitmap(originalImage);
        var color = overlayColor ?? Color.FromArgb(100, 255, 0, 0);

        using var g = Graphics.FromImage(overlay);
        using var brush = new SolidBrush(color);

        var mask = result.Mask;
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
    /// <param name="result">The segmentation result.</param>
    /// <returns>A new bitmap with transparent background.</returns>
    public static Bitmap ExtractForeground(Bitmap originalImage, ImageSegmentationResult result)
    {
        ArgumentNullException.ThrowIfNull(originalImage);
        ArgumentNullException.ThrowIfNull(result);

        var output = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
        var mask = result.Mask;

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

    #region Private Helper Methods

    private static Bitmap ResizeWithPadding(Bitmap originalBitmap, int targetWidth, int targetHeight)
    {
        // Determine the scaling factor to fit the image within the target dimensions
        float scale = Math.Min((float)targetWidth / originalBitmap.Width, (float)targetHeight / originalBitmap.Height);

        // Calculate the new width and height based on the scaling factor
        int scaledWidth = (int)(originalBitmap.Width * scale);
        int scaledHeight = (int)(originalBitmap.Height * scale);

        // Center the image within the target dimensions
        int offsetX = (targetWidth - scaledWidth) / 2;
        int offsetY = (targetHeight - scaledHeight) / 2;

        // Create a new bitmap with the target dimensions and a white background for padding
        Bitmap paddedBitmap = new(targetWidth, targetHeight);
        using (Graphics graphics = Graphics.FromImage(paddedBitmap))
        {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(originalBitmap, offsetX, offsetY, scaledWidth, scaledHeight);
        }

        return paddedBitmap;
    }

    private static Tensor<float> PreprocessBitmapFloat(Bitmap bitmap, Tensor<float> input)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            int stride = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(stride) * height;
            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 3;
                    byte blue = rgbValues[index];
                    byte green = rgbValues[index + 1];
                    byte red = rgbValues[index + 2];

                    // Normalize to [0, 1] range
                    input[0, 0, y, x] = red / 255f;
                    input[0, 1, y, x] = green / 255f;
                    input[0, 2, y, x] = blue / 255f;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        return input;
    }

    private static Tensor<byte> PreprocessBitmapQuantized(Bitmap bitmap, Tensor<byte> input)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            int stride = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(stride) * height;
            byte[] rgbValues = new byte[bytes];

            Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 3;
                    byte blue = rgbValues[index];
                    byte green = rgbValues[index + 1];
                    byte red = rgbValues[index + 2];

                    // Quantized model expects raw byte values (0-255)
                    input[0, 0, y, x] = red;
                    input[0, 1, y, x] = green;
                    input[0, 2, y, x] = blue;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        return input;
    }

    private static IEnumerable<float> DequantizeOutput(Tensor<byte> quantizedOutput)
    {
        // Convert UInt8 output to float (0-255 -> 0.0-1.0)
        var outputArray = quantizedOutput.ToArray();
        var floatOutput = new float[outputArray.Length];
        
        for (int i = 0; i < outputArray.Length; i++)
        {
            floatOutput[i] = outputArray[i] / 255f;
        }
        
        return floatOutput;
    }

    private static byte[] GetForegroundMask(IEnumerable<float> output, int maskWidth, int maskHeight, int originalWidth, int originalHeight)
    {
        float[] tensorData = output.ToArray();
        byte[] mask = new byte[originalWidth * originalHeight * 4]; // RGBA format

        // Compute scaling factor (inverse of what was used in ResizeWithPadding)
        float scale = Math.Min((float)maskWidth / originalWidth, (float)maskHeight / originalHeight);

        // Compute padding applied during resizing
        int scaledWidth = (int)(originalWidth * scale);
        int scaledHeight = (int)(originalHeight * scale);
        int offsetX = (maskWidth - scaledWidth) / 2;
        int offsetY = (maskHeight - scaledHeight) / 2;

        Parallel.For(0, originalHeight, y =>
        {
            for (int x = 0; x < originalWidth; x++)
            {
                float scaledX = (float)x / originalWidth * scaledWidth + offsetX;
                float scaledY = (float)y / originalHeight * scaledHeight + offsetY;

                scaledX = Math.Clamp(scaledX, 0, maskWidth - 1);
                scaledY = Math.Clamp(scaledY, 0, maskHeight - 1);

                int x0 = (int)Math.Floor(scaledX);
                int x1 = Math.Min(x0 + 1, maskWidth - 1);
                int y0 = (int)Math.Floor(scaledY);
                int y1 = Math.Min(y0 + 1, maskHeight - 1);

                float xWeight = scaledX - x0;
                float yWeight = scaledY - y0;

                // Foreground probability (channel 1)
                float fg00 = tensorData[y0 * maskWidth + x0 + maskWidth * maskHeight];
                float fg10 = tensorData[y0 * maskWidth + x1 + maskWidth * maskHeight];
                float fg01 = tensorData[y1 * maskWidth + x0 + maskWidth * maskHeight];
                float fg11 = tensorData[y1 * maskWidth + x1 + maskWidth * maskHeight];

                float fgProb = (fg00 * (1 - xWeight) + fg10 * xWeight) * (1 - yWeight) +
                               (fg01 * (1 - xWeight) + fg11 * xWeight) * yWeight;

                // Background probability (channel 0)
                float bgProb = (tensorData[y0 * maskWidth + x0] * (1 - xWeight) + tensorData[y0 * maskWidth + x1] * xWeight) * (1 - yWeight) +
                               (tensorData[y1 * maskWidth + x0] * (1 - xWeight) + tensorData[y1 * maskWidth + x1] * xWeight) * yWeight;

                int index = (y * originalWidth + x) * 4;

                if (fgProb < bgProb)
                {
                    // Background (white with full alpha to indicate background)
                    mask[index] = 255;     // R
                    mask[index + 1] = 255; // G
                    mask[index + 2] = 255; // B
                    mask[index + 3] = 255; // A
                }
                else
                {
                    // Foreground (transparent)
                    mask[index] = 0;
                    mask[index + 1] = 0;
                    mask[index + 2] = 0;
                    mask[index + 3] = 0;
                }
            }
        });

        return mask;
    }

    #endregion

    /// <summary>
    /// Disposes the image segmenter and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
