# Contoso.AI.ImageSegmenterSINet

[![Build Status](https://github.com/contoso/ImageSegmenter-SINet/workflows/Build,%20Test,%20and%20Publish/badge.svg)](https://github.com/contoso/ImageSegmenter-SINet/actions)
[![NuGet](https://img.shields.io/nuget/v/Contoso.AI.ImageSegmenterSINet.svg)](https://www.nuget.org/packages/Contoso.AI.ImageSegmenterSINet/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

AI-powered image segmentation library using Qualcomm's **SINet (Salient Image Network)** ONNX model. Automatically separates foreground from background in images with NPU hardware acceleration on compatible Windows devices.

## ✨ Features

- **Foreground/Background Segmentation** - Automatically detect and separate subjects from backgrounds
- **NPU Hardware Acceleration** - Leverages Qualcomm QNN Execution Provider for fast inference on NPU-equipped devices
- **Automatic Model Download** - SINet model downloads automatically at build time
- **Easy-to-Use API** - Simple async factory pattern with `CreateAsync()`
- **Multiple Output Formats**:
  - Raw segmentation mask
  - Overlay visualization
  - Foreground extraction with transparent background
- **NuGet Package** - Easy integration into any .NET project

## 📋 Requirements

- **Windows 10** SDK 19041 or later
- **.NET 8.0** or later
- **NPU** (optional but recommended) - Qualcomm Snapdragon X Elite or compatible device for hardware acceleration
- Falls back to CPU execution if NPU is not available

## 🚀 Quick Start

### Installation

```bash
dotnet add package Contoso.AI.ImageSegmenterSINet
```

Or via Package Manager Console:

```powershell
Install-Package Contoso.AI.ImageSegmenterSINet
```

### Basic Usage

```csharp
using Contoso.AI;
using System.Drawing;

// Create segmenter instance (downloads model on first build)
using var segmenter = await ImageSegmenterSINet.CreateAsync();

// Load an image
using var bitmap = new Bitmap("photo.jpg");

// Perform segmentation
var result = segmenter.SegmentImage(bitmap);

// Check results
Console.WriteLine($"Foreground detected: {result.HasForeground}");
Console.WriteLine($"Foreground coverage: {result.Mask.GetForegroundPercentage():P2}");

// Extract foreground with transparent background
using var foreground = result.ExtractForeground(bitmap);
foreground.Save("foreground.png");
```

### Check Feature Availability

```csharp
// Check if the feature is ready
var readyState = ImageSegmenterSINet.GetReadyState();

if (readyState != AIFeatureReadyState.Ready)
{
    // Prepare the feature (downloads dependencies if needed)
    var prepResult = await ImageSegmenterSINet.EnsureReadyAsync();
    
    if (prepResult.Status != AIFeatureReadyResultState.Success)
    {
        Console.WriteLine($"Failed to prepare: {prepResult.ExtendedError?.Message}");
        return;
    }
}

// Now safe to create the segmenter
using var segmenter = await ImageSegmenterSINet.CreateAsync();
```

## 📖 API Reference

### ImageSegmenterSINet Class

| Method | Description |
|--------|-------------|
| `GetReadyState()` | Static. Returns `AIFeatureReadyState` indicating if the feature can be used |
| `EnsureReadyAsync()` | Static. Downloads and prepares all required dependencies |
| `CreateAsync()` | Static factory. Creates and initializes a new `ImageSegmenterSINet` instance |
| `SegmentImage(Bitmap)` | Performs segmentation on the provided image |

### ImageSegmentationResult Class

| Property/Method | Type | Description |
|-----------------|------|-------------|
| `Mask` | `SegmentationMask` | The segmentation mask data |
| `OriginalWidth` | `int` | Original image width |
| `OriginalHeight` | `int` | Original image height |
| `HasForeground` | `bool` | Whether any foreground was detected |
| `CreateMaskOverlay(Bitmap, Color?)` | `Bitmap` | Creates a visualization overlay with the mask on the original image |
| `ExtractForeground(Bitmap)` | `Bitmap` | Extracts foreground with transparent background |

### SegmentationMask Class

| Method/Property | Description |
|-----------------|-------------|
| `Width`, `Height` | Mask dimensions in pixels |
| `Data` | Raw RGBA byte array (4 bytes per pixel) |
| `IsForeground(x, y)` | Check if a specific pixel is foreground |
| `IsBackground(x, y)` | Check if a specific pixel is background |
| `CountForegroundPixels()` | Count of foreground pixels |
| `CountBackgroundPixels()` | Count of background pixels |
| `GetForegroundPercentage()` | Foreground as percentage (0.0 - 1.0) |

## 🏗️ Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/contoso/ImageSegmenter-SINet.git
cd ImageSegmenter-SINet

# Restore and build (model downloads automatically)
dotnet restore
dotnet build

# Run the console test
dotnet run --project Contoso.AI.ImageSegmenterSINet.ConsoleTest
```

### Project Structure

```
ImageSegmenter-SINet/
├── Contoso.AI.ImageSegmenter.sln
├── README.md
├── LICENSE
├── Contoso.AI.ImageSegmenterSINet/
│   ├── Contoso.AI.ImageSegmenterSINet.csproj
│   ├── ImageSegmenterSINet.cs
│   ├── ImageSegmentationResult.cs
│   ├── SegmentationMask.cs
│   ├── README.md
│   └── build/
│       └── Contoso.AI.ImageSegmenterSINet.targets
├── Contoso.AI.ImageSegmenterSINet.ConsoleTest/
│   ├── Contoso.AI.ImageSegmenterSINet.ConsoleTest.csproj
│   ├── Program.cs
│   └── Assets/
│       └── SampleImage.png
└── .github/
    └── workflows/
        └── build-and-publish.yml
```

### Model Information

This project uses the **SINet (Salient Image Network)** model from Qualcomm:

- **Source**: [Qualcomm SINet on Hugging Face](https://huggingface.co/qualcomm/SINet)
- **Version**: v0.45.0
- **Format**: ONNX (float)
- **License**: See model repository for license terms

The model is automatically downloaded during the first build and cached in the `obj/Models` directory.

## 🧪 Testing

The console test project demonstrates the full workflow:

```bash
dotnet run --project Contoso.AI.ImageSegmenterSINet.ConsoleTest
```

This will:
1. Check feature availability
2. Create the segmenter
3. Process a sample image
4. Output statistics and visualization files

## 📦 Creating NuGet Package

```bash
# Build the package
dotnet pack Contoso.AI.ImageSegmenterSINet -c Release

# The package will be in bin/Release/Contoso.AI.ImageSegmenterSINet.0.1.0-beta.nupkg
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Qualcomm](https://www.qualcomm.com/) for the SINet ONNX model
- [ONNX Runtime](https://onnxruntime.ai/) for the inference engine
- [Windows ML](https://docs.microsoft.com/en-us/windows/ai/windows-ml/) for NPU acceleration support

---

**Made with ❤️ by Contoso**
