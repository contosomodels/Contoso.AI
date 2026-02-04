# Creating New AI Model Projects

This guide provides step-by-step instructions for creating new AI model NuGet packages in the **Contoso.AI mono-repo**. Use this as a template when building new AI feature libraries for different model types (e.g., Human Pose Detection, Object Classification, Text Recognition, etc.).

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Mono-Repo Structure](#mono-repo-structure)
3. [Naming Conventions](#naming-conventions)
4. [Adding a New Project](#adding-a-new-project)
5. [Core Library Project Setup](#core-library-project-setup)
6. [Implementing the AI Feature Class](#implementing-the-ai-feature-class)
7. [Creating Result Models](#creating-result-models)
8. [Build-Time Model Download](#build-time-model-download)
9. [Console Test Project](#console-test-project)
10. [NuGet Package Configuration](#nuget-package-configuration)
11. [CI/CD Pipeline](#cicd-pipeline)
12. [Documentation](#documentation)
13. [Checklist](#checklist)

---

## Overview

This mono-repo contains multiple AI model projects, each following a consistent pattern:

- **Shared AIFeatureReadyState APIs** for checking feature availability
- **Async initialization** with `EnsureReadyAsync()` for dependency management
- **Factory pattern** with `CreateAsync()` for instance creation
- **Automatic model download** at build time via MSBuild targets
- **NuGet packaging** with transitive build targets for consuming projects
- **Independent CI/CD workflows** that only run when their specific project changes

### Key Dependencies

| Package | Purpose |
|---------|---------|
| `Contoso.AI.AIFeatureCore` | Shared types: `AIFeatureReadyState`, `AIFeatureReadyResult`, `AIFeatureReadyResultState` |
| `Microsoft.WindowsAppSDK` | Windows ML APIs, `ExecutionProviderCatalog` |
| `System.Drawing.Common` | Image processing (if applicable) |
| `System.Numerics.Tensors` | Tensor operations for ONNX models |

---

## Mono-Repo Structure

The repository is organized with each AI feature as a top-level folder containing its own solution:

```
Contoso.AI/                              # Root of mono-repo
├── .github/
│   └── workflows/
│       ├── person-detector-build-and-publish.yml
│       ├── politeness-analyzer-build-and-publish.yml
│       └── [feature-name]-build-and-publish.yml    # Add new workflows here
├── CREATING_NEW_AI_MODEL_PROJECTS.md    # This guide
├── README.md                            # Repo-level README
├── PersonDetector/
│   ├── Contoso.AI.PersonDetector.sln
│   ├── README.md
│   ├── Contoso.AI.PersonDetector/
│   │   ├── Contoso.AI.PersonDetector.csproj
│   │   ├── PersonDetector.cs
│   │   ├── PersonDetectionResult.cs
│   │   ├── Detection.cs
│   │   ├── README.md
│   │   └── build/
│   │       └── Contoso.AI.PersonDetector.targets
│   └── Contoso.AI.PersonDetector.ConsoleTest/
│       ├── Contoso.AI.PersonDetector.ConsoleTest.csproj
│       ├── Program.cs
│       └── Assets/
├── PolitenessAnalyzer/
│   ├── Contoso.AI.PolitenessAnalyzer.sln
│   ├── README.md
│   ├── Contoso.AI.PolitenessAnalyzer/
│   │   └── ...
│   └── Contoso.AI.PolitenessAnalyzer.ConsoleTest/
│       └── ...
└── [FeatureName]/                       # New projects follow this pattern
    ├── Contoso.AI.[FeatureName].sln
    ├── README.md
    ├── Contoso.AI.[FeatureName]/
    │   └── ...
    └── Contoso.AI.[FeatureName].ConsoleTest/
        └── ...
```

### Key Points

- **Workflows are at the repo root**: All GitHub Actions workflows live in `.github/workflows/` at the repository root
- **Each project has its own solution**: Projects are self-contained with their own `.sln` file
- **Path-filtered CI/CD**: Each workflow only triggers when its specific folder changes

---

## Naming Conventions

### Project/Package Naming

Follow this pattern: `Contoso.AI.[FeatureName]`

| Model Type | Folder Name | Package Name |
|------------|-------------|--------------|
| Person Detection | `PersonDetector` | `Contoso.AI.PersonDetector` |
| Politeness Analysis | `PolitenessAnalyzer` | `Contoso.AI.PolitenessAnalyzer` |
| Human Pose Detection | `PoseDetector` | `Contoso.AI.PoseDetector` |
| Object Classification | `ObjectClassifier` | `Contoso.AI.ObjectClassifier` |
| Text Recognition | `TextRecognizer` | `Contoso.AI.TextRecognizer` |
| Face Detection | `FaceDetector` | `Contoso.AI.FaceDetector` |
| Image Segmentation | `ImageSegmenter` | `Contoso.AI.ImageSegmenter` |

### Class Naming

| Component | Pattern | Example |
|-----------|---------|---------|
| Main feature class | `[FeatureName]` | `PoseDetector`, `TextRecognizer` |
| Result class | `[FeatureName]Result` | `PoseDetectionResult`, `TextRecognitionResult` |
| Detection/item class | `[ItemType]` or `Detection` | `Pose`, `DetectedText`, `Detection` |

### Namespace

All classes should be in the `Contoso.AI` namespace for consistency:

```csharp
namespace Contoso.AI
{
    public class PoseDetector : IDisposable { }
    public class PoseDetectionResult { }
    public class Pose { }
}
```

---

## Adding a New Project

When adding a new AI feature to the mono-repo, follow these steps:

### 1. Create the Folder Structure

Create a new top-level folder for your feature:

```
[FeatureName]/
├── Contoso.AI.[FeatureName].sln
├── README.md
├── LICENSE
├── Contoso.AI.[FeatureName]/
│   ├── Contoso.AI.[FeatureName].csproj
│   ├── [FeatureName].cs
│   ├── [FeatureName]Result.cs
│   ├── [ItemType].cs
│   ├── README.md
│   └── build/
│       └── Contoso.AI.[FeatureName].targets
└── Contoso.AI.[FeatureName].ConsoleTest/
    ├── Contoso.AI.[FeatureName].ConsoleTest.csproj
    ├── Program.cs
    └── Assets/
        └── SampleImage.png
```

### 2. Add GitHub Actions Workflow

Create a new workflow file at `.github/workflows/[feature-name]-build-and-publish.yml` (see [CI/CD Pipeline](#cicd-pipeline) section).

### 3. Update Repo-Level Documentation

Add your new project to the root `README.md` if one exists.

---

## Core Library Project Setup

### Project File (.csproj)

Create the `.csproj` file with NuGet packaging configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Platforms>AnyCPU;x64</Platforms>

    <!-- NuGet Package Configuration -->
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Contoso.AI.[FeatureName]</PackageId>
    <Version>0.1.0-beta</Version>
    <Authors>Contoso</Authors>
    <Company>Contoso</Company>
    <Description>AI-powered [feature description] using ONNX Runtime. Model downloads automatically at build time. Requires Windows 10 SDK 19041 or later.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- Platform Requirements -->
    <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Contoso.AI.AIFeatureCore" Version="0.0.1-beta" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251106002" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
    <PackageReference Include="System.Numerics.Tensors" Version="9.0.9" />
  </ItemGroup>

  <!-- Include .targets file in NuGet package for consuming projects -->
  <ItemGroup>
    <None Include="build\Contoso.AI.[FeatureName].targets" Pack="true" PackagePath="build" />
    <None Include="build\Contoso.AI.[FeatureName].targets" Pack="true" PackagePath="buildTransitive" />
    <None Include="README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

---

## Implementing the AI Feature Class

### Required Static Methods

Every AI feature class must implement these static methods using the shared `AIFeatureReadyState` APIs:

```csharp
using Microsoft.ML.OnnxRuntime;
using Microsoft.Windows.AI.MachineLearning;
using System.Diagnostics;

namespace Contoso.AI
{
    /// <summary>
    /// Provides [feature description] capabilities using [Model Name] on NPU hardware acceleration.
    /// </summary>
    public class [FeatureName] : IDisposable
    {
        private readonly OrtEnv _env;
        private readonly InferenceSession _session;
        private readonly string _inputName;
        
        // Model-specific constants
        private const int InputWidth = 640;      // Adjust per model requirements
        private const int InputHeight = 640;     // Adjust per model requirements
        private const float ConfidenceThreshold = 0.8f;
        private const string ModelPath = "Models/[ModelFolderName]/model.onnx";

        // Private constructor - use CreateAsync() factory method
        private [FeatureName](OrtEnv env, InferenceSession session)
        {
            _env = env;
            _session = session;
            
            if (session != null)
            {
                var inputMeta = _session.InputMetadata.First();
                _inputName = inputMeta.Key;
            }
        }

        /// <summary>
        /// Gets the ready state of the [feature] feature.
        /// </summary>
        /// <returns>The ready state indicating if the feature can be used.</returns>
        public static AIFeatureReadyState GetReadyState()
        {
            try
            {
                // Check if model file exists
                if (!File.Exists(ModelPath))
                {
                    Debug.WriteLine($"[[FeatureName]] Model file not found: {ModelPath}");
                    return AIFeatureReadyState.NotReady;
                }

                // Check if Windows ML EP catalog is available
                var catalog = ExecutionProviderCatalog.GetDefault();
                var qnnProvider = catalog.FindAllProviders()
                    .FirstOrDefault(i => i.Name == "QNNExecutionProvider");

                if (qnnProvider == null)
                {
                    Debug.WriteLine("[[FeatureName]] QNN Execution Provider not found");
                    return AIFeatureReadyState.NotReady;
                }

                if (qnnProvider.ReadyState == ExecutionProviderReadyState.NotPresent)
                {
                    Debug.WriteLine("[[FeatureName]] QNN Execution Provider not present, needs download");
                    return AIFeatureReadyState.NotReady;
                }

                return AIFeatureReadyState.Ready;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[[FeatureName]] Error checking ready state: {ex.Message}");
                return AIFeatureReadyState.NotReady;
            }
        }

        /// <summary>
        /// Ensures the [feature] feature is ready by downloading necessary dependencies.
        /// </summary>
        /// <returns>A task containing the preparation result.</returns>
        public static async Task<AIFeatureReadyResult> EnsureReadyAsync()
        {
            try
            {
                // Check if model file exists
                if (!File.Exists(ModelPath))
                {
                    throw new FileNotFoundException($"Model file not found: {ModelPath}");
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
                    Debug.WriteLine("[[FeatureName]] Downloading QNN Execution Provider...");
                    await qnnProvider.EnsureReadyAsync();
                }

                // Register all EPs with ONNX Runtime
                await catalog.RegisterCertifiedAsync();

                Debug.WriteLine("[[FeatureName]] Feature is ready");
                return AIFeatureReadyResult.Success();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[[FeatureName]] Failed to ensure ready: {ex.Message}");
                return AIFeatureReadyResult.Failed(ex);
            }
        }

        /// <summary>
        /// Creates a new [feature] instance with NPU acceleration.
        /// </summary>
        /// <returns>A task containing the initialized instance.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the model file is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when initialization fails.</exception>
        public static async Task<[FeatureName]> CreateAsync()
        {
            if (!File.Exists(ModelPath))
            {
                throw new FileNotFoundException($"Model file not found: {ModelPath}");
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

            // Prefer QNN NPU, fall back to CPU
            var ep = epDevices.FirstOrDefault(i => 
                i.EpName == "QNNExecutionProvider" && 
                i.HardwareDevice.Type == OrtHardwareDeviceType.NPU);
            
            if (ep == null)
            {
                ep = epDevices.First(i => i.EpName == "CPUExecutionProvider");
            }

            // Configure session options
            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider(env, new[] { ep }, null);

            // Create the inference session
            var session = new InferenceSession(ModelPath, sessionOptions);

            Debug.WriteLine($"[[FeatureName]] Created with model: {ModelPath}");
            return new [FeatureName](env, session);
        }

        /// <summary>
        /// Main detection/recognition method - customize for your model.
        /// </summary>
        public [FeatureName]Result Detect[Items](Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            // TODO: Implement model-specific inference logic
            // 1. Preprocess input (resize, normalize, convert to tensor)
            // 2. Run inference
            // 3. Post-process outputs (decode predictions, NMS, etc.)
            // 4. Return results
            
            return new [FeatureName]Result();
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
```

---

## Creating Result Models

### Result Container Class

```csharp
using System.Collections.Generic;

namespace Contoso.AI
{
    /// <summary>
    /// Represents the result of [feature] on an image.
    /// </summary>
    public class [FeatureName]Result
    {
        /// <summary>
        /// Gets or sets the list of detected [items] in the image.
        /// </summary>
        public List<[ItemType]> [Items] { get; set; } = new();

        /// <summary>
        /// Gets or sets the total count of detected [items].
        /// </summary>
        public int Total[Items]Count { get; set; }
    }
}
```

### Individual Item/Detection Class

```csharp
using System.Drawing;

namespace Contoso.AI
{
    /// <summary>
    /// Represents a detected [item] with bounding box and confidence.
    /// </summary>
    public class [ItemType]
    {
        /// <summary>
        /// Gets or sets the class name of the detected object.
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence score (0.0 to 1.0).
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Gets or sets the bounding box of the detected object.
        /// </summary>
        public RectangleF BoundingBox { get; set; }

        // Add model-specific properties here, e.g.:
        // public List<Point> Keypoints { get; set; }  // For pose detection
        // public string RecognizedText { get; set; }  // For text recognition
    }
}
```

---

## Build-Time Model Download

### MSBuild Targets File

Create `build/Contoso.AI.[FeatureName].targets`:

```xml
<Project>
  <PropertyGroup>
    <!-- Configure these for your model -->
    <ModelUrl>https://huggingface.co/[org]/[model]/resolve/[commit]/[filename].zip</ModelUrl>
    <ModelZipFileName>[model-name].zip</ModelZipFileName>
    <ModelFileName>model.onnx</ModelFileName>
    <ModelDataFileName>model.data</ModelDataFileName>
    <ModelFolderName>[Model-Folder-Name]</ModelFolderName>
    
    <!-- Download to obj directory to avoid source control pollution -->
    <ModelDirectory>$(MSBuildProjectDirectory)\obj\Models</ModelDirectory>
    <ModelExtractDirectory>$(ModelDirectory)\$(ModelFolderName)</ModelExtractDirectory>
  </PropertyGroup>

  <!-- Download model at build time -->
  <Target Name="Download[FeatureName]Model" BeforeTargets="BeforeBuild">
    <PropertyGroup>
      <ModelZipFilePath>$(ModelDirectory)\$(ModelZipFileName)</ModelZipFilePath>
      <ModelFilePath>$(ModelExtractDirectory)\$(ModelFileName)</ModelFilePath>
      <ModelDataFilePath>$(ModelExtractDirectory)\$(ModelDataFileName)</ModelDataFilePath>
    </PropertyGroup>
    
    <!-- Create directories -->
    <MakeDir Directories="$(ModelDirectory)" Condition="!Exists('$(ModelDirectory)')" />
    <MakeDir Directories="$(ModelExtractDirectory)" Condition="!Exists('$(ModelExtractDirectory)')" />
    
    <!-- Status messages -->
    <Message Text="[Contoso.AI.[FeatureName]] Checking for model at: $(ModelFilePath)" Importance="high" />
    <Message Text="[Contoso.AI.[FeatureName]] Model already exists, skipping download." 
             Importance="high" Condition="Exists('$(ModelFilePath)')" />
    
    <!-- Download ZIP file -->
    <DownloadFile 
      SourceUrl="$(ModelUrl)" 
      DestinationFolder="$(ModelDirectory)"
      DestinationFileName="$(ModelZipFileName)"
      Condition="!Exists('$(ModelFilePath)')"
      SkipUnchangedFiles="true" />
      
    <Message Text="[Contoso.AI.[FeatureName]] Model ZIP downloaded successfully" 
             Importance="high" 
             Condition="!Exists('$(ModelFilePath)') AND Exists('$(ModelZipFilePath)')" />
    
    <!-- Extract ZIP file -->
    <Unzip 
      SourceFiles="$(ModelZipFilePath)" 
      DestinationFolder="$(ModelDirectory)\temp"
      Condition="!Exists('$(ModelFilePath)') AND Exists('$(ModelZipFilePath)')" />
      
    <!-- Move extracted files -->
    <ItemGroup>
      <ExtractedModelFiles Include="$(ModelDirectory)\temp\**\*.onnx" />
      <ExtractedModelFiles Include="$(ModelDirectory)\temp\**\*.data" />
    </ItemGroup>
    
    <Copy
      SourceFiles="@(ExtractedModelFiles)"
      DestinationFolder="$(ModelExtractDirectory)"
      Condition="!Exists('$(ModelFilePath)')" />
      
    <!-- Clean up -->
    <RemoveDir Directories="$(ModelDirectory)\temp" Condition="Exists('$(ModelDirectory)\temp')" />
      
    <Message Text="[Contoso.AI.[FeatureName]] Model extracted successfully" 
             Importance="high" Condition="Exists('$(ModelFilePath)')" />
    
    <!-- Add model files to output -->
    <ItemGroup>
      <Content Include="$(ModelFilePath)" Condition="Exists('$(ModelFilePath)')">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <Link>Models\$(ModelFolderName)\$(ModelFileName)</Link>
      </Content>
      <Content Include="$(ModelDataFilePath)" Condition="Exists('$(ModelDataFilePath)')">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <Link>Models\$(ModelFolderName)\$(ModelDataFileName)</Link>
      </Content>
    </ItemGroup>
  </Target>
</Project>
```

---

## Console Test Project

### Test Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <Platforms>AnyCPU;x64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Contoso.AI.[FeatureName]\Contoso.AI.[FeatureName].csproj" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.WindowsDesktop.App" />
  </ItemGroup>

  <!-- Import model download targets -->
  <Import Project="..\Contoso.AI.[FeatureName]\build\Contoso.AI.[FeatureName].targets" />

</Project>
```

### Test Program (Program.cs)

```csharp
using Contoso.AI;
using System.Drawing;

Console.WriteLine("=== Contoso.AI.[FeatureName] Console Test ===");
Console.WriteLine();

const string imagePath = "Assets/SampleImage.png";

if (!File.Exists(imagePath))
{
    Console.WriteLine($"ERROR: Sample image not found at: {imagePath}");
    return;
}

Console.WriteLine($"Loading image: {imagePath}");
Console.WriteLine();

try
{
    // Check if the feature is ready
    Console.WriteLine("Checking if [FeatureName] is ready...");
    var readyState = [FeatureName].GetReadyState();
    
    if (readyState != AIFeatureReadyState.Ready)
    {
        Console.WriteLine($"[FeatureName] is not ready. State: {readyState}");
        Console.WriteLine("Attempting to prepare the feature...");
        
        var readyResult = await [FeatureName].EnsureReadyAsync();
        
        if (readyResult.Status != AIFeatureReadyResultState.Success)
        {
            Console.WriteLine($"ERROR: Failed to prepare: {readyResult.ExtendedError?.Message}");
            return;
        }
        
        Console.WriteLine("[FeatureName] is now ready!");
    }
    else
    {
        Console.WriteLine("[FeatureName] is ready!");
    }
    
    Console.WriteLine();

    // Create the detector/recognizer instance
    Console.WriteLine("Creating [FeatureName] instance...");
    using var detector = await [FeatureName].CreateAsync();
    Console.WriteLine("[FeatureName] created successfully!");
    Console.WriteLine();

    // Load and process the image
    Console.WriteLine("Loading and analyzing image...");
    using var bitmap = new Bitmap(imagePath);
    Console.WriteLine($"Image size: {bitmap.Width}x{bitmap.Height}");
    Console.WriteLine();

    // Run detection/recognition
    var result = detector.Detect[Items](bitmap);

    // Output results
    Console.WriteLine("=== RESULTS ===");
    Console.WriteLine($"Total [items] detected: {result.Total[Items]Count}");
    Console.WriteLine();

    if (result.Total[Items]Count > 0)
    {
        Console.WriteLine("Detected [items]:");
        for (int i = 0; i < result.[Items].Count; i++)
        {
            var item = result.[Items][i];
            Console.WriteLine($"  [Item] #{i + 1}:");
            Console.WriteLine($"    Confidence: {item.Confidence:P2}");
            Console.WriteLine($"    Bounding Box: X={item.BoundingBox.X:F1}, Y={item.BoundingBox.Y:F1}");
            Console.WriteLine();
        }
    }
    else
    {
        Console.WriteLine("No [items] detected in the image.");
    }

    Console.WriteLine("=== TEST COMPLETE ===");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
```

---

## NuGet Package Configuration

### Key Configuration Points

1. **Package ID**: `Contoso.AI.[FeatureName]`
2. **Version**: Use semantic versioning with `-beta` suffix for pre-release
3. **Include Targets**: Pack the `.targets` file in both `build` and `buildTransitive` paths

```xml
<!-- In .csproj -->
<ItemGroup>
  <None Include="build\Contoso.AI.[FeatureName].targets" Pack="true" PackagePath="build" />
  <None Include="build\Contoso.AI.[FeatureName].targets" Pack="true" PackagePath="buildTransitive" />
  <None Include="README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### Publishing Commands

```bash
# Build the package (from the project's folder)
dotnet pack Contoso.AI.[FeatureName] -c Release

# Push to NuGet
dotnet nuget push "bin/Release/*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

---

## CI/CD Pipeline

In a mono-repo, all GitHub Actions workflows live at the repository root in `.github/workflows/`. Each project gets its own workflow file with **path filters** to ensure it only runs when that specific project changes.

### Workflow File Location

Create `.github/workflows/[feature-name]-build-and-publish.yml` at the **repository root**:

```yaml
name: [FeatureName] - Build, Test, and Publish

on:
  push:
    branches: [ main ]
    paths:
      - '[FeatureName]/**'
      - '.github/workflows/[feature-name]-build-and-publish.yml'
  pull_request:
    branches: [ main ]
    paths:
      - '[FeatureName]/**'
      - '.github/workflows/[feature-name]-build-and-publish.yml'

env:
  DOTNET_VERSION: '8.0.x'
  NUGET_SOURCE: 'https://api.nuget.org/v3/index.json'

defaults:
  run:
    working-directory: [FeatureName]

jobs:
  build-and-test:
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build solution
      run: dotnet build --configuration Release --no-restore
    
    - name: Run console tests
      run: dotnet run --project Contoso.AI.[FeatureName].ConsoleTest --configuration Release --no-build
    
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: [feature-name]-test-results
        path: [FeatureName]/TestResults/
        if-no-files-found: ignore

  publish-nuget:
    needs: build-and-test
    runs-on: windows-latest
    permissions:
      contents: write
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Generate version number
      id: get-version
      shell: pwsh
      run: |
        # Generate version based on run number: 0.1.RUN_NUMBER-beta
        $runNumber = "${{ github.run_number }}"
        $newVersion = "0.1.$runNumber-beta"
        
        Write-Host "Generated version: $newVersion"
        echo "NEW_VERSION=$newVersion" >> $env:GITHUB_OUTPUT
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build solution
      run: dotnet build --configuration Release --no-restore
    
    - name: Pack NuGet package
      run: dotnet pack Contoso.AI.[FeatureName]\Contoso.AI.[FeatureName].csproj --configuration Release --no-build -p:PackageVersion=${{ steps.get-version.outputs.NEW_VERSION }}
    
    - name: Publish to NuGet
      run: dotnet nuget push "Contoso.AI.[FeatureName]\bin\Release\*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source ${{ env.NUGET_SOURCE }} --skip-duplicate
      env:
        NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
    
    - name: Create GitHub Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: [feature-name]-v${{ steps.get-version.outputs.NEW_VERSION }}
        release_name: [FeatureName] v${{ steps.get-version.outputs.NEW_VERSION }}
        body: |
          Automated release of [FeatureName] version ${{ steps.get-version.outputs.NEW_VERSION }}
          
          NuGet package published: https://www.nuget.org/packages/Contoso.AI.[FeatureName]/${{ steps.get-version.outputs.NEW_VERSION }}
        draft: false
        prerelease: true
```

### Key Mono-Repo Workflow Features

| Feature | Purpose |
|---------|---------|
| `paths` filter | Only triggers when the specific project folder changes |
| `working-directory` default | All commands run inside the project folder |
| Unique artifact names | Prevents conflicts (e.g., `person-detector-test-results`) |
| Prefixed release tags | Avoids tag collisions (e.g., `person-detector-v0.1.5-beta`) |
| Workflow file in paths | Also triggers when the workflow itself is modified |

---

## Documentation

### Required Documentation Files

1. **Root README.md** - Repo-level overview listing all projects
2. **Project README.md** - Full documentation for each project with badges, quick start, API reference
3. **Library README.md** - NuGet package readme (shorter, focused on usage)
4. **CREATING_NEW_AI_MODEL_PROJECTS.md** - This guide (at repo root)

### README Template Sections

- Badges (Build, NuGet version, downloads)
- Features list
- Platform requirements
- Quick start / installation
- Usage examples
- API reference
- Development setup
- Testing instructions
- Contributing guidelines
- License

---

## Checklist

Use this checklist when adding a new AI model project to the mono-repo:

### Setup
- [ ] Create top-level folder with naming pattern `[FeatureName]/`
- [ ] Create solution `Contoso.AI.[FeatureName].sln`
- [ ] Create library project `Contoso.AI.[FeatureName]/`
- [ ] Create console test project `Contoso.AI.[FeatureName].ConsoleTest/`
- [ ] Add entries to `.gitignore` if needed (model files in `obj/`)

### Core Library
- [ ] Configure `.csproj` with NuGet package settings
- [ ] Add required package references
- [ ] Implement main feature class with:
  - [ ] `GetReadyState()` static method
  - [ ] `EnsureReadyAsync()` static method
  - [ ] `CreateAsync()` factory method
  - [ ] Main detection/recognition method
  - [ ] `IDisposable` implementation
- [ ] Create result model class
- [ ] Create item/detection model class

### Build System
- [ ] Create `build/Contoso.AI.[FeatureName].targets`
- [ ] Configure model download URL
- [ ] Test model download on first build
- [ ] Verify model copies to output directory

### Testing
- [ ] Create test `Program.cs`
- [ ] Add sample image(s) to `Assets/`
- [ ] Verify end-to-end workflow

### Packaging
- [ ] Include `.targets` in NuGet package (build + buildTransitive)
- [ ] Include README.md in package
- [ ] Set appropriate version number
- [ ] Test NuGet package locally

### Documentation
- [ ] Write project-level README.md
- [ ] Write library README.md (for NuGet)
- [ ] Document API with XML comments
- [ ] Update repo-level README.md to list new project

### CI/CD (Mono-Repo Specific)
- [ ] Create workflow at `.github/workflows/[feature-name]-build-and-publish.yml`
- [ ] Configure path filters for the project folder
- [ ] Set `working-directory` to the project folder
- [ ] Use unique artifact names (prefixed with feature name)
- [ ] Use prefixed release tags (e.g., `[feature-name]-v1.0.0`)
- [ ] Ensure `NUGET_API_KEY` secret is configured in repo settings
- [ ] Test automated builds by pushing a change

---

## Example: Human Pose Detection

Here's how you would apply this guide for a Human Pose Detection model:

| Item | Value |
|------|-------|
| Folder Name | `PoseDetector` |
| Package Name | `Contoso.AI.PoseDetector` |
| Main Class | `PoseDetector` |
| Result Class | `PoseDetectionResult` |
| Item Class | `Pose` (with `Keypoints` property) |
| Model Folder | `HRNet_w8a8` (or similar) |
| Main Method | `DetectPoses(Bitmap bitmap)` |
| Workflow File | `.github/workflows/pose-detector-build-and-publish.yml` |
| Release Tag Prefix | `pose-detector-v` |

The `Pose` class might include additional properties:

```csharp
public class Pose
{
    public float Confidence { get; set; }
    public RectangleF BoundingBox { get; set; }
    public List<Keypoint> Keypoints { get; set; } = new();
}

public class Keypoint
{
    public string Name { get; set; }  // e.g., "LeftElbow", "RightKnee"
    public PointF Position { get; set; }
    public float Confidence { get; set; }
}
```

---

**Made with ❤️ by Contoso**
