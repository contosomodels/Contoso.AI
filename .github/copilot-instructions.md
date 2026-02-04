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
- **Package names**: `Contoso.AI.[FeatureName]` (e.g., `Contoso.AI.PersonDetector`)
- **Folder names**: `[FeatureName]` (e.g., `PersonDetector`)
- **Workflow files**: `[feature-name]-build-and-publish.yml` (kebab-case)

### Required API Pattern

Every AI feature class must implement these static methods:

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
