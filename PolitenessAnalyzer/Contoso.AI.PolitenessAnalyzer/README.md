# Contoso.AI.PolitenessAnalyzer

AI-powered politeness analysis for text using ONNX Runtime and a BERT-based model.

> **Windows Only**: This package requires Windows 10 SDK version 19041 or later and uses Windows-specific AI APIs.

## Features

- Analyzes text for politeness level (Polite, Somewhat Polite, Neutral, Impolite)
- Performance and Efficiency modes for different hardware configurations
- Async API for non-blocking operations
- Automatic model download at build time

## Installation

```bash
dotnet add package Contoso.AI.PolitenessAnalyzer
```

**Platform Requirements:**
- Windows 10 version 2004 (build 19041) or later
- Not compatible with: Linux, macOS, or older Windows versions

## Model Download

**Important**: This package uses a ~418 MB ONNX model that is **automatically downloaded at build time**.

- The model is downloaded to `obj/Models/polite-guard-model.onnx` (not tracked by git)
- Download happens only once (cached for subsequent builds)
- The model file is automatically copied to your output directory (`bin/.../Models/`)
- **No need to add to `.gitignore`** - the `obj/` folder is already ignored by default

## Usage

```csharp
using Contoso.AI;

// Initialize (call once at startup)
var readyResult = await PolitenessAnalyzer.EnsureReadyAsync();
if (readyResult.Status != AIFeatureReadyResultState.Success)
{
    // Handle initialization failure
    return;
}

// Create analyzer instance
var analyzer = await PolitenessAnalyzer.CreateAsync(PerformanceMode.Performance);

// Analyze text
var result = await analyzer.AnalyzeAsync("Thank you so much for your help!");

Console.WriteLine($"Politeness Level: {result.Level}");
Console.WriteLine($"Description: {result.Description}");
Console.WriteLine($"Inference Time: {result.InferenceTimeMs}ms");

// Don't forget to dispose
analyzer.Dispose();
```

## Performance Modes

- `PerformanceMode.Performance` - Optimized for speed (uses GPU/NPU if available)
- `PerformanceMode.Efficiency` - Optimized for power efficiency (uses CPU)

## Model Information

- **Source**: [Intel/polite-guard on HuggingFace](https://huggingface.co/Intel/polite-guard)
- **Size**: ~418 MB
- **License**: Apache 2.0
- **Type**: BERT-based ONNX model

## Requirements

- .NET 8.0 or later
- Windows 10 SDK 19041 or later
- Internet connection for initial model download

## License

MIT
