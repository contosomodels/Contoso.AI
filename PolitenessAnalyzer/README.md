# Contoso.AI.PolitenessAnalyzer

[![Build, Test, and Publish](https://github.com/contosomodels/PolitenessAnalyzer/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/contosomodels/PolitenessAnalyzer/actions/workflows/build-and-publish.yml)
[![NuGet](https://img.shields.io/nuget/v/Contoso.AI.PolitenessAnalyzer.svg)](https://www.nuget.org/packages/Contoso.AI.PolitenessAnalyzer/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Contoso.AI.PolitenessAnalyzer.svg)](https://www.nuget.org/packages/Contoso.AI.PolitenessAnalyzer/)

AI-powered politeness analyzer for text using ONNX Runtime and the Intel polite-guard model. Built for .NET 8 on Windows with the Windows App SDK.

## ✨ Features

- 🤖 **AI-Powered Analysis** - Uses Intel's polite-guard ONNX model for accurate politeness detection
- 📊 **Four Politeness Levels** - Polite, Somewhat Polite, Neutral, Impolite
- ⚡ **Fast Inference** - Optimized ONNX Runtime execution
- 🪟 **Windows Native** - Built with Windows App SDK for optimal performance
- 📦 **Easy Integration** - Simple NuGet package with automatic model download
- 🔒 **Type Safe** - Full C# type safety with nullable reference types

## 🚀 Quick Start

### Installation

```bash
dotnet add package Contoso.AI.PolitenessAnalyzer
```

### Basic Usage

```csharp
using Contoso.AI;

// Initialize the analyzer (one-time setup)
await PolitenessAnalyzer.EnsureReadyAsync();
var analyzer = await PolitenessAnalyzer.CreateAsync();

// Analyze text
var result = await analyzer.AnalyzeAsync("Thank you so much for your help!");

Console.WriteLine($"Level: {result.Level}");           // Polite
Console.WriteLine($"Description: {result.Description}"); 
Console.WriteLine($"Inference Time: {result.InferenceTimeMs}ms");

// Clean up
analyzer.Dispose();
```

## 📋 Politeness Levels

| Level | Description | Example |
|-------|-------------|---------|
| **Polite** | Considerate, respectful, courteous phrases | "Thank you so much for your help!" |
| **Somewhat Polite** | Generally respectful but lacking warmth | "Could you please send the report?" |
| **Neutral** | Straightforward, factual, no emotional tone | "The meeting is at 3pm." |
| **Impolite** | Disrespectful, rude, dismissive | "This is completely unacceptable!" |

## 📁 Project Structure

```
PolitenessAnalyzer/
├── Contoso.AI.PolitenessAnalyzer/           # Main library
│   ├── PolitenessAnalyzer.cs                # Core analysis engine
│   ├── PolitenessLevel.cs                   # Politeness level enum
│   ├── PolitenessAnalysisResponse.cs        # Response model
│   └── BertTokenizer.cs                     # BERT tokenization
├── Contoso.AI.PolitenessAnalyzer.ConsoleTest/ # Console test app
│   └── Program.cs                           # Test runner
├── .github/workflows/                       # CI/CD pipelines
│   └── build-and-publish.yml                # Automated build & publish
└── README.md                                # This file
```

## ⚙️ Requirements

- **Platform**: Windows 10 (19041) or later
- **Runtime**: .NET 8.0
- **SDK**: Windows App SDK 1.8+
- **Architecture**: x64, x86, or ARM64

## 🛠️ Development Setup

### Prerequisites

1. [Visual Studio 2022](https://visualstudio.microsoft.com/) or later
2. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
3. Windows 10 SDK (19041 or later)

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/contosomodels/PolitenessAnalyzer.git
cd PolitenessAnalyzer

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet run --project Contoso.AI.PolitenessAnalyzer.ConsoleTest
```

### Model Download

The ONNX model (Intel polite-guard) is automatically downloaded during the first build. It's cached in the `obj/Models` directory to avoid re-downloading on subsequent builds.

## 🧪 Testing

The project includes a console-based test runner with predefined test cases:

```bash
dotnet run --project Contoso.AI.PolitenessAnalyzer.ConsoleTest
```

**Expected Output:**
```
=== Politeness Analyzer Console Test ===

Initializing analyzer...
✓ Analyzer initialized successfully

Test 1: "Thank you so much for your help!"
Expected: Polite
Actual: Polite
✓ PASSED

...

✓ All tests passed!
```

## 📦 NuGet Package

The package is automatically built and published via GitHub Actions on every push to `main`.

### Version Scheme

- **Format**: `0.1.[BUILD_NUMBER]-beta`
- **Example**: `0.1.42-beta`

Versions increment automatically based on the GitHub Actions run number.

### Manual Publishing

If you need to publish manually:

```bash
dotnet pack Contoso.AI.PolitenessAnalyzer -c Release
dotnet nuget push "bin/Release/*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Pull Request Process

- PRs trigger automated builds and tests
- All tests must pass before merging
- Code should follow existing style conventions
- Update documentation as needed

## 🔄 CI/CD Pipeline

The project uses GitHub Actions for automated builds:

- **On Pull Request**: Builds and runs tests (no publishing)
- **On Push to Main**: Builds, tests, and publishes to NuGet

See [GITHUB_ACTIONS_SETUP.md](GITHUB_ACTIONS_SETUP.md) for setup details.

## 📚 API Reference

### `PolitenessAnalyzer`

#### Static Methods

```csharp
// Check if analyzer is ready
AIFeatureReadyState GetReadyState()

// Initialize shared resources
Task<AIFeatureReadyResult> EnsureReadyAsync()

// Create analyzer instance
Task<PolitenessAnalyzer> CreateAsync()
```

#### Instance Methods

```csharp
// Analyze text for politeness
Task<PolitenessAnalysisResponse> AnalyzeAsync(string text)

// Clean up resources
void Dispose()
```

### `PolitenessAnalysisResponse`

```csharp
public class PolitenessAnalysisResponse
{
    public PolitenessLevel Level { get; init; }
    public string Description { get; init; }
    public long InferenceTimeMs { get; init; }
}
```

### `PolitenessLevel` Enum

```csharp
public enum PolitenessLevel
{
    Polite,
    SomewhatPolite,
    Neutral,
    Impolite
}
```

## 💡 Use Cases

- **Customer Support**: Analyze customer messages for sentiment
- **Content Moderation**: Flag impolite or disrespectful content
- **Email Assistants**: Suggest tone improvements
- **Chat Applications**: Monitor conversation tone
- **HR Tools**: Analyze workplace communication

## 🔧 Advanced Configuration

### Custom Execution Providers

The analyzer automatically selects the best available execution provider (CPU by default). Future versions may support GPU acceleration.

### Model Path Override

The model is loaded from `Models\polite-guard-model.onnx` in the application's base directory. This can be customized by modifying `PolitenessAnalyzer.GetModelPath()`.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Intel** - For the [polite-guard model](https://huggingface.co/Intel/polite-guard)
- **Microsoft** - For ONNX Runtime and Windows App SDK
- **Community** - For feedback and contributions

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/contosomodels/PolitenessAnalyzer/issues)
- **Discussions**: [GitHub Discussions](https://github.com/contosomodels/PolitenessAnalyzer/discussions)
- **NuGet**: [Package Page](https://www.nuget.org/packages/Contoso.AI.PolitenessAnalyzer/)

## 🗺️ Roadmap

- [ ] Add GPU execution provider support
- [ ] Support for batch processing
- [ ] Add more pre-built test cases
- [ ] Performance benchmarking suite
- [ ] Multi-language support
- [ ] Web API wrapper example

---

**Made with ❤️ by Contoso**
