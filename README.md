# Contoso.AI

A mono-repo containing AI-powered NuGet packages for Windows applications. Each package provides easy-to-use APIs for machine learning features with automatic model download and NPU hardware acceleration.

## 📦 Projects

| Project | Description | NuGet |
|---------|-------------|-------|
| [PersonDetector](PersonDetector/) | Detect people in images using YOLO-based object detection | [![NuGet](https://img.shields.io/nuget/v/Contoso.AI.PersonDetector)](https://www.nuget.org/packages/Contoso.AI.PersonDetector) |
| [PolitenessAnalyzer](PolitenessAnalyzer/) | Analyze text for politeness levels using BERT-based NLP | [![NuGet](https://img.shields.io/nuget/v/Contoso.AI.PolitenessAnalyzer)](https://www.nuget.org/packages/Contoso.AI.PolitenessAnalyzer) |

## ✨ Features

- **Automatic Model Download** - ONNX models download automatically at build time
- **NPU Acceleration** - Hardware acceleration via Windows ML and QNN Execution Provider
- **Consistent APIs** - All packages follow the same patterns (`GetReadyState()`, `EnsureReadyAsync()`, `CreateAsync()`)
- **NuGet Ready** - Each project publishes as a standalone NuGet package

## 🎯 Requirements

- Windows 10 SDK 19041 or later
- .NET 8.0
- NPU-capable hardware (optional, falls back to CPU)

## 🚀 Quick Start

Install any package via NuGet:

```bash
dotnet add package Contoso.AI.PersonDetector
dotnet add package Contoso.AI.PolitenessAnalyzer
```

All packages follow the same usage pattern:

```csharp
// 1. Check if the feature is ready
var readyState = PersonDetector.GetReadyState();

// 2. Ensure dependencies are downloaded (if needed)
if (readyState != AIFeatureReadyState.Ready)
{
    await PersonDetector.EnsureReadyAsync();
}

// 3. Create an instance
using var detector = await PersonDetector.CreateAsync();

// 4. Use the feature
var result = detector.DetectPersons(bitmap);
```

## 🏗️ Repository Structure

```
Contoso.AI/
├── .github/
│   └── workflows/           # CI/CD workflows (path-filtered per project)
├── PersonDetector/          # Person detection project
│   ├── Contoso.AI.PersonDetector/
│   └── Contoso.AI.PersonDetector.ConsoleTest/
├── PolitenessAnalyzer/      # Politeness analysis project
│   ├── Contoso.AI.PolitenessAnalyzer/
│   └── Contoso.AI.PolitenessAnalyzer.ConsoleTest/
├── CREATING_NEW_AI_MODEL_PROJECTS.md
└── README.md
```

Each project is self-contained with its own solution file and can be built independently.

## 🔧 Development

### Building a Project

```bash
cd PersonDetector
dotnet build
```

The first build will automatically download the required ONNX model.

### Running Tests

```bash
cd PersonDetector
dotnet run --project Contoso.AI.PersonDetector.ConsoleTest
```

### Adding a New Project

See [CREATING_NEW_AI_MODEL_PROJECTS.md](CREATING_NEW_AI_MODEL_PROJECTS.md) for detailed instructions on adding new AI feature packages to this repo.

## 🔄 CI/CD

Each project has its own GitHub Actions workflow that:
- Only triggers when that project's folder changes
- Builds and tests on every PR
- Publishes to NuGet on merge to `main`
- Creates GitHub releases with prefixed tags

## 📄 License

MIT

---

**Made with ❤️ by Contoso**
