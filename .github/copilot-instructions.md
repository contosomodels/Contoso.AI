# GitHub Copilot Instructions for Contoso.AI

This is a mono-repo containing multiple AI-powered NuGet packages for Windows applications. Each package provides machine learning features with automatic ONNX model download and NPU hardware acceleration.

## Repository Structure

```
Contoso.AI/
├── .github/
│   └── workflows/           # CI/CD workflows (one per project, path-filtered)
├── PersonDetector/          # Person detection using YOLO
├── PolitenessAnalyzer/      # Text politeness analysis using BERT
├── [FeatureName]/           # Future projects follow this pattern
├── CREATING_NEW_AI_MODEL_PROJECTS.md  # Detailed guide for new projects
└── README.md
```

Each project folder contains:
- `Contoso.AI.[FeatureName].sln` - Solution file
- `Contoso.AI.[FeatureName]/` - Core library project
- `Contoso.AI.[FeatureName].ConsoleTest/` - Test console app
- `README.md` - Project documentation

## Key Patterns and Conventions

### Naming
- **Namespaces**: Always use `Contoso.AI`
- **Package names**: 
  - Single model per task: `Contoso.AI.[FeatureName]` (e.g., `Contoso.AI.PersonDetector`)
  - Multiple models per task: `Contoso.AI.[TaskType].ModelName` (e.g., `Contoso.AI.ImageSegmenter.SINet`)
  - Base interface package: `Contoso.AI.[TaskType]` (e.g., `Contoso.AI.ImageSegmenter`) when multiple models exist
- **Folder names**: 
  - Single model: `[FeatureName]` (e.g., `PersonDetector`)
  - Multiple models: `[TaskType]ModelName` (e.g., `ImageSegmenterSINet`)
- **Workflow files**: `[feature-name]-build-and-publish.yml` (kebab-case)

### Required API Pattern

Every AI feature class must implement these static methods:

**For single-model features:**
```csharp
namespace Contoso.AI
{
    public class [FeatureName] : IDisposable
    {
        // Check if ready without side effects
        public static AIFeatureReadyState GetReadyState();
        
        // Download dependencies if needed
        public static async Task<AIFeatureReadyResult> EnsureReadyAsync();
        
        // Factory method to create instance
        public static async Task<[FeatureName]> CreateAsync();
        
        // Main feature method(s) - naming varies by feature
        public [FeatureName]Result Detect[Items](Bitmap bitmap);
        // or
        public [FeatureName]Response Analyze(string text);
        
        public void Dispose();
    }
}
```

**For multiple models sharing the same task type:**

Create a base interface library (`Contoso.AI.[TaskType]`) with:
```csharp
namespace Contoso.AI
{
    public interface I[TaskType] : IDisposable
    {
        // Task-specific methods
        [TaskType]Result Process(Bitmap bitmap);
    }
}
```

Then each model implementation (`Contoso.AI.[TaskType].ModelName`) implements both the interface and the standard pattern:
```csharp
namespace Contoso.AI
{
    public class [TaskType]ModelName : I[TaskType]
    {
        public static AIFeatureReadyState GetReadyState();
        public static async Task<AIFeatureReadyResult> EnsureReadyAsync();
        public static async Task<I[TaskType]> CreateAsync();
        
        public [TaskType]Result Process(Bitmap bitmap)
        {
            // Model-specific implementation
        }
        
        public void Dispose();
    }
}
```

**Example usage with interface:**
```csharp
// Check if the feature is ready
var readyState = ImageSegmenterSINet.GetReadyState();

if (readyState != AIFeatureReadyState.Ready)
{
    var readyResult = await ImageSegmenterSINet.EnsureReadyAsync();
}

// Create segmenter instance - returns interface type
using IImageSegmenter segmenter = await ImageSegmenterSINet.CreateAsync();

// Perform task - interface method
var result = segmenter.SegmentImage(bitmap);
```

This pattern allows users to swap models by changing only the class name (e.g., `ImageSegmenterSINet` → `ImageSegmenterUNet`) without modifying the rest of their code.

### Dependencies

Standard package references for AI projects:
```xml
<PackageReference Include="Contoso.AI.AIFeatureCore" Version="0.0.1-beta" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251106002" />
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />
<PackageReference Include="System.Numerics.Tensors" Version="9.0.9" />
```

### Target Framework
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
```

## CI/CD Workflows

All workflows are in `.github/workflows/` at the repo root. Each project has its own workflow with:

1. **Path filters** - Only trigger on changes to that project's folder
2. **Working directory** - Set to the project folder via `defaults.run.working-directory`
3. **Unique artifact names** - Prefixed with feature name (e.g., `person-detector-test-results`)
4. **Prefixed release tags** - e.g., `person-detector-v0.1.5-beta`

Example trigger configuration:
```yaml
on:
  push:
    branches: [ main ]
    paths:
      - '[FeatureName]/**'
      - '.github/workflows/[feature-name]-build-and-publish.yml'
```

## Common Tasks

### Creating a New AI Project

1. Create the folder structure following `CREATING_NEW_AI_MODEL_PROJECTS.md`
2. Implement the standard API pattern (GetReadyState, EnsureReadyAsync, CreateAsync)
3. Create MSBuild targets for model download in `build/Contoso.AI.[FeatureName].targets`
4. Add workflow at `.github/workflows/[feature-name]-build-and-publish.yml`
5. Update root `README.md` to list the new project

### Modifying a Workflow

- Workflows are at `.github/workflows/`, not inside project folders
- Always include the workflow file itself in the `paths` filter
- Use `working-directory: [FeatureName]` for all build commands

### Adding Model Download

Models are downloaded via MSBuild targets at build time:
- Targets file: `[FeatureName]/Contoso.AI.[FeatureName]/build/Contoso.AI.[FeatureName].targets`
- Models download to `obj/Models/` to avoid source control
- Models are copied to output directory at `Models/[ModelFolderName]/`

## Code Style

- Use C# 12 features (file-scoped namespaces, primary constructors where appropriate)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use async/await for all I/O operations
- Implement `IDisposable` for classes holding native resources
- Use XML documentation comments on public APIs

## Testing

Each project has a console test app that:
1. Checks `GetReadyState()`
2. Calls `EnsureReadyAsync()` if needed
3. Creates an instance via `CreateAsync()`
4. Processes a sample asset from `Assets/`
5. Outputs results to console

## Ingesting Models from AI Dev Gallery

The [Microsoft AI Dev Gallery](https://github.com/microsoft/ai-dev-gallery) provides sample implementations for various HuggingFace ONNX Runtime models. Follow this process to create new Contoso.AI libraries based on AI Dev Gallery samples:

### 1. Clone the AI Dev Gallery Repository

```bash
git clone https://github.com/microsoft/ai-dev-gallery.git /tmp/ai-dev-gallery
cd /tmp/ai-dev-gallery
```

### 2. Locate the Sample Code

Browse the samples at `AIDevGallery/Samples/Open Source Models/` (organized by category like Embeddings, Image Classification, etc.).

Example: For Semantic Search, the sample is at:
```
AIDevGallery/Samples/Open Source Models/Embeddings/SemanticSearch.xaml.cs
```

### 3. Inspect the GallerySample Attribute

Each sample has a `[GallerySample]` attribute that provides critical information:

```csharp
[GallerySample(
    Name = "Semantic Search",
    Model1Types = [ModelType.EmbeddingModel],
    Scenario = ScenarioType.TextSemanticSearch,
    SharedCode = [
        SharedCodeEnum.EmbeddingGenerator,
        SharedCodeEnum.EmbeddingModelInput,
        SharedCodeEnum.TokenizerExtensions,
        SharedCodeEnum.DeviceUtils,
        SharedCodeEnum.StringData
    ],
    NugetPackageReferences = [
        "System.Numerics.Tensors",
        "Microsoft.ML.Tokenizers",
        "Microsoft.Extensions.AI",
        "Microsoft.SemanticKernel.Connectors.InMemory"
    ],
    Id = "41391b3f-f143-4719-a171-b0ce9c4cdcd6",
    Icon = "\uE8D4")]
```

**Key fields:**
- **Model1Types**: Type of model needed (check model definitions)
- **SharedCode**: List of shared utility files required from `AIDevGallery/Samples/SharedCode/`
- **NugetPackageReferences**: Additional NuGet packages to include
- **Scenario**: The task type (helps determine naming)

### 4. Gather SharedCode Files

All shared code is in: `AIDevGallery/Samples/SharedCode/` (and subfolders)

For each `SharedCodeEnum` value, find the corresponding C# file:
- `EmbeddingGenerator` → `EmbeddingGenerator.cs`
- `TokenizerExtensions` → `TokenizerExtensions.cs`
- `DeviceUtils` → `DeviceUtils.cs`

Copy these files into your new Contoso.AI project and adapt them as needed.

### 5. Locate Model Definitions

Model information is stored at:
```
AIDevGallery/Samples/Definitions/Models/
```

Find the JSON file for your model type (e.g., `EmbeddingModel.json`, `ImageClassificationModel.json`).

**Example model definition:**
```json
{
  "Id": "sentence-transformers/all-MiniLM-L6-v2",
  "Name": "all-MiniLM-L6-v2",
  "Description": "Sentence embedding model...",
  "HuggingFaceUrl": "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2",
  "FileNames": [
    "model.onnx",
    "tokenizer.json",
    "tokenizer_config.json"
  ]
}
```

### 6. Get Absolute HuggingFace Download Links

**IMPORTANT:** Always use absolute HuggingFace links with commit hashes for reproducibility:

**Format:**
```
https://huggingface.co/{org}/{model}/resolve/{commit_hash}/{file_path}?download=true
```

**Steps to get the commit hash:**
1. Go to the HuggingFace model page (e.g., `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2`)
2. Click on "Files and versions" tab
3. Click on the commit you want to use (typically the latest)
4. Copy the commit hash from the URL or commit list

**Example links:**
```xml
<!-- ❌ DON'T use generic evergreen links -->
<ModelUrl>https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/model.onnx</ModelUrl>

<!-- ✅ DO use absolute links with commit hash -->
<ModelUrl>https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/d83dd3760b5bfe921f2fe125446b17bf0b7eda8c/onnx/model.onnx?download=true</ModelUrl>
```

### 7. Determine Package Naming

**Single Model per Task Type:**
```
Contoso.AI.[FeatureName]
Example: Contoso.AI.PersonDetector
```

**Multiple Models for Same Task Type:**

Create two packages:
1. **Base Interface Library:** `Contoso.AI.[TaskType]`
   - Contains the interface (e.g., `IImageSegmenter`, `ITextEmbedder`)
   - Contains shared result types
   - No model-specific code

2. **Model Implementation:** `Contoso.AI.[TaskType].ModelName`
   - References the base interface library
   - Implements the interface with a specific model
   - Follows standard API pattern

**Example for multiple image segmentation models:**
```
Base: Contoso.AI.ImageSegmenter
  - IImageSegmenter interface
  - ImageSegmentationResult class

Model 1: Contoso.AI.ImageSegmenter.SINet
  - ImageSegmenterSINet class implementing IImageSegmenter
  
Model 2: Contoso.AI.ImageSegmenter.UNet
  - ImageSegmenterUNet class implementing IImageSegmenter
```

**Folder structure:**
```
ImageSegmenter/                                    # Base library folder
├── Contoso.AI.ImageSegmenter.sln                  # Shared solution
├── Contoso.AI.ImageSegmenter/                     # Base interface library
│   └── IImageSegmenter.cs
├── Contoso.AI.ImageSegmenter.SINet/               # SINet model implementation
├── Contoso.AI.ImageSegmenter.SINet.ConsoleTest/
├── Contoso.AI.ImageSegmenter.UNet/                # UNet model implementation (future)
└── Contoso.AI.ImageSegmenter.UNet.ConsoleTest/
```

### 8. Implementation Workflow

1. **Create project structure** following `CREATING_NEW_AI_MODEL_PROJECTS.md`
2. **Copy and adapt SharedCode files** from AI Dev Gallery
3. **Implement the standard API pattern** (GetReadyState, EnsureReadyAsync, CreateAsync)
4. **Create MSBuild targets** for model download with absolute HuggingFace URLs
5. **Add required NuGet packages** from the GallerySample attribute
6. **Create console test app** following existing patterns
7. **Add GitHub Actions workflow** with path filters
8. **Update repository README** to list the new project

### 9. Example: Creating a Text Embedder from AI Dev Gallery

**Scenario:** Create `Contoso.AI.TextEmbedder.MiniLM` based on the Semantic Search sample.

**Steps:**
```bash
# 1. Clone AI Dev Gallery
git clone https://github.com/microsoft/ai-dev-gallery.git /tmp/ai-dev-gallery

# 2. Inspect the sample
cat /tmp/ai-dev-gallery/AIDevGallery/Samples/Open\ Source\ Models/Embeddings/SemanticSearch.xaml.cs

# 3. Copy SharedCode files
cp /tmp/ai-dev-gallery/AIDevGallery/Samples/SharedCode/EmbeddingGenerator.cs \
   TextEmbedderMiniLM/Contoso.AI.TextEmbedder.MiniLM/

# 4. Check model definition
cat /tmp/ai-dev-gallery/AIDevGallery/Samples/Definitions/Models/EmbeddingModel.json

# 5. Get commit hash from HuggingFace
# Visit: https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/commits/main
# Copy latest commit: d83dd3760b5bfe921f2fe125446b17bf0b7eda8c
```

**Create MSBuild targets with absolute URLs:**
```xml
<Target Name="DownloadModels" BeforeTargets="BeforeBuild">
  <DownloadFile 
    SourceUrl="https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/d83dd3760b5bfe921f2fe125446b17bf0b7eda8c/onnx/model.onnx?download=true"
    DestinationFolder="$(MSBuildProjectDirectory)\obj\Models\MiniLM\" />
</Target>
```

### Tips for AI Dev Gallery Integration

- **Start with simpler samples** (single-model scenarios) before tackling complex multi-model tasks
- **Review SharedCode carefully** - some utilities may need adaptation for our API patterns
- **Test with AI Dev Gallery's test assets** first, then replace with your own
- **Check NuGet package versions** - AI Dev Gallery might use different versions
- **Document the source** - add comments linking back to the original AI Dev Gallery sample
- **Consider creating the base interface first** if you anticipate multiple models for a task type
