# Contoso.AI.ImageSegmenterSINet

AI-powered image segmentation using Qualcomm's SINet (Salient Image Network) ONNX model. Separates foreground from background in images with NPU hardware acceleration.

## Quick Start

```csharp
using Contoso.AI;
using System.Drawing;

// Create segmenter (model downloads automatically at build time)
using var segmenter = await ImageSegmenterSINet.CreateAsync();

// Segment an image
using var bitmap = new Bitmap("photo.jpg");
var result = segmenter.SegmentImage(bitmap);

// Extract foreground with transparent background
using var foreground = ImageSegmenterSINet.ExtractForeground(bitmap, result);
foreground.Save("foreground.png");
```

## Features

- ✅ Foreground/background segmentation using SINet
- ✅ NPU hardware acceleration (QNN Execution Provider)
- ✅ Automatic model download at build time (both float and quantized models)
- ✅ Intelligent model selection: quantized for NPU, float for CPU
- ✅ Easy-to-use async factory pattern
- ✅ Multiple output formats (mask, overlay, extracted foreground)

## Requirements

- Windows 10 SDK 19041+
- .NET 8.0+
- NPU recommended for best performance (falls back to CPU automatically)

## Model Selection

The library automatically downloads two ONNX models at build time:
- **Quantized (int8) model** - Optimized for QNN NPU acceleration
- **Float (fp32) model** - Optimized for CPU execution

At runtime, `CreateAsync()` intelligently selects:
- Quantized model when QNN NPU is available
- Float model when falling back to CPU

This ensures optimal performance and compatibility across different hardware configurations.

## API

| Method | Description |
|--------|-------------|
| `ImageSegmenterSINet.GetReadyState()` | Check if feature is available |
| `ImageSegmenterSINet.EnsureReadyAsync()` | Prepare dependencies |
| `ImageSegmenterSINet.CreateAsync()` | Create segmenter instance |
| `SegmentImage(Bitmap)` | Perform segmentation |
| `CreateMaskOverlay(...)` | Visualize segmentation |
| `ExtractForeground(...)` | Extract foreground with transparency |

## License

MIT License - see LICENSE file for details.
