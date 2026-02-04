using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Windows.AI.MachineLearning;
using System.Runtime.Versioning;

namespace Contoso.AI;

[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class PolitenessAnalyzer : IDisposable
{
    private static AIFeatureReadyState _readyState = AIFeatureReadyState.NotReady;
    private static readonly object _readyStateLock = new();
    private static Task<AIFeatureReadyResult>? _ensureReadyTask;
    private static string? _modelPath;
    private static BertTokenizer? _sharedTokenizer;
    private static OrtEnv? _sharedOrtEnv;
    private static List<OrtEpDevice>? _availableDevices;
    
    private InferenceSession? _inferenceSession;
    private const int MaxSequenceLength = 512;

    public event EventHandler<string>? InitializationStatusChanged;

    private PolitenessAnalyzer()
    {
    }

    public static AIFeatureReadyState GetReadyState()
    {
        lock (_readyStateLock)
        {
            return _readyState;
        }
    }

    public static Task<AIFeatureReadyResult> EnsureReadyAsync()
    {
        lock (_readyStateLock)
        {
            if (_ensureReadyTask != null)
            {
                return _ensureReadyTask;
            }

            if (_readyState == AIFeatureReadyState.Ready)
            {
                return Task.FromResult(AIFeatureReadyResult.Success());
            }

            _readyState = AIFeatureReadyState.Initializing;
            _ensureReadyTask = InitializeSharedResourcesAsync();
            return _ensureReadyTask;
        }
    }

    private static async Task<AIFeatureReadyResult> InitializeSharedResourcesAsync()
    {
        try
        {
            // Don't use WinML EPs for now
            //await ExecutionProviderCatalog.GetDefault().EnsureAndRegisterCertifiedAsync();

            _sharedTokenizer = await Task.Run(() => new BertTokenizer());

            _modelPath = GetModelPath();

            await Task.Run(() =>
            {
                EnvironmentCreationOptions envOptions = new()
                {
                    logId = "PolitenessGuard",
                    logLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_FATAL // Suppress warnings about CPU detection
                };

                _sharedOrtEnv = OrtEnv.CreateInstanceWithOptions(ref envOptions);
                _availableDevices = _sharedOrtEnv.GetEpDevices().ToList();
            });

            lock (_readyStateLock)
            {
                _readyState = AIFeatureReadyState.Ready;
            }

            return AIFeatureReadyResult.Success();
        }
        catch (Exception ex)
        {
            lock (_readyStateLock)
            {
                _readyState = AIFeatureReadyState.NotReady;
                _ensureReadyTask = null;
            }
            return AIFeatureReadyResult.Failed(ex);
        }
    }

    private static string GetModelPath()
    {
        // Look for the model in the Models subdirectory
        string modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "polite-guard-model.onnx");
        
        if (File.Exists(modelPath))
        {
            return modelPath;
        }

        throw new FileNotFoundException($"Model file not found at: {modelPath}");
    }

    public static async Task<PolitenessAnalyzer> CreateAsync()
    {
        if (_readyState != AIFeatureReadyState.Ready)
        {
            var result = await EnsureReadyAsync();
            if (result.Status != AIFeatureReadyResultState.Success)
            {
                throw result.ExtendedError ?? new InvalidOperationException("Failed to initialize PolitenessAnalyzer");
            }
        }

        var analyzer = new PolitenessAnalyzer();
        
        analyzer._inferenceSession = await Task.Run(() => analyzer.CreateInferenceSession());

        return analyzer;
    }

    public async Task<PolitenessAnalysisResponse> AnalyzeAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PolitenessAnalysisResponse
            {
                Level = PolitenessLevel.Neutral,
                Description = "No text to analyze",
                InferenceTimeMs = 0
            };
        }

        if (_inferenceSession == null)
        {
            throw new InvalidOperationException("Analyzer not initialized properly");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var level = await Task.Run(() => RunInference(text));
        stopwatch.Stop();
        
        var description = GetPolitenessDescription(level);

        return new PolitenessAnalysisResponse
        {
            Level = level,
            Description = description,
            InferenceTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    private InferenceSession CreateInferenceSession()
    {
        if (_sharedOrtEnv == null || _modelPath == null)
        {
            throw new InvalidOperationException("Shared resources not initialized");
        }

        var sessionOptions = new SessionOptions();
        var cpuEp = _sharedOrtEnv.GetEpDevices().First(i => i.EpName == "CPUExecutionProvider");
        var epOptions = new Dictionary<string, string>();
        sessionOptions.AppendExecutionProvider(_sharedOrtEnv, new[] { cpuEp }, epOptions);

        return new InferenceSession(_modelPath, sessionOptions);
    }

    private PolitenessLevel RunInference(string text)
    {
        if (_sharedTokenizer == null)
        {
            throw new InvalidOperationException("Tokenizer not initialized");
        }

        var encoding = _sharedTokenizer.Encode(text, MaxSequenceLength);

        var inputIds = new DenseTensor<long>(new[] { 1, encoding.InputIds.Length });
        var attentionMask = new DenseTensor<long>(new[] { 1, encoding.AttentionMask.Length });
        var tokenTypeIds = new DenseTensor<long>(new[] { 1, encoding.TokenTypeIds.Length });

        for (int i = 0; i < encoding.InputIds.Length; i++)
        {
            inputIds[0, i] = encoding.InputIds[i];
            attentionMask[0, i] = encoding.AttentionMask[i];
            tokenTypeIds[0, i] = encoding.TokenTypeIds[i];
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
        };

        using var results = _inferenceSession!.Run(inputs);
        
        var logits = results.First().AsEnumerable<float>().ToArray();
        
        var probabilities = Softmax(logits);
        
        int predictedClass = GetMaxIndex(probabilities);

        return MapToPolitenessLevel(predictedClass, probabilities);
    }

    private PolitenessLevel MapToPolitenessLevel(int predictedClass, float[] probabilities)
    {
        // Based on Intel polite-guard model card on HuggingFace:
        // The model outputs 4 classes corresponding to:
        // - polite
        // - somewhat polite  
        // - neutral
        // - impolite
        //
        // Based on empirical testing, the mapping appears to be:
        // Class 0: neutral
        // Class 1: somewhat polite (rarely activated by model)
        // Class 2: polite
        // Class 3: impolite
        
        float confidence = probabilities[predictedClass];

        // If we have exactly 2 classes (binary classification)
        if (probabilities.Length == 2)
        {
            // Binary: Class 0 = Polite, Class 1 = Impolite
            if (predictedClass == 0)
            {
                return confidence > 0.8f ? PolitenessLevel.Polite : PolitenessLevel.SomewhatPolite;
            }
            else
            {
                return confidence > 0.8f ? PolitenessLevel.Impolite : PolitenessLevel.Neutral;
            }
        }
        // If we have 4 classes (Intel polite-guard model)
        else if (probabilities.Length == 4)
        {
            // Direct mapping based on observed model behavior
            switch (predictedClass)
            {
                case 0:
                    // Class 0: neutral
                    return PolitenessLevel.Neutral;
                
                case 1:
                    // Class 1: somewhat polite (model rarely uses this class)
                    return PolitenessLevel.SomewhatPolite;
                
                case 2:
                    // Class 2: polite
                    // If confidence is low or Class 3 (impolite) is competitive,
                    // it might actually be somewhat polite with mixed signals
                    if (confidence < 0.5f || probabilities[3] > 0.25f || probabilities[1] > 0.15f)
                    {
                        return PolitenessLevel.SomewhatPolite;
                    }
                    return PolitenessLevel.Polite;
                
                case 3:
                    // Class 3: impolite
                    return PolitenessLevel.Impolite;
                
                default:
                    return PolitenessLevel.Neutral;
            }
        }
        // Fallback for other cases
        else
        {
            return PolitenessLevel.Neutral;
        }
    }

    private float[] Softmax(float[] values)
    {
        var maxVal = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxVal)).ToArray();
        var sum = exp.Sum();
        return exp.Select(e => (float)(e / sum)).ToArray();
    }

    private int GetMaxIndex(float[] values)
    {
        int maxIndex = 0;
        float maxValue = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > maxValue)
            {
                maxValue = values[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    private string GetPolitenessDescription(PolitenessLevel level)
    {
        return level switch
        {
            PolitenessLevel.Polite => "Text is considerate and shows respect and good manners, often including courteous phrases and a friendly tone.",
            PolitenessLevel.SomewhatPolite => "Text is generally respectful but lacks warmth or formality, communicating with a decent level of courtesy.",
            PolitenessLevel.Neutral => "Text is straightforward and factual, without emotional undertones or specific attempts at politeness.",
            PolitenessLevel.Impolite => "Text is disrespectful or rude, often blunt or dismissive, showing a lack of consideration for the recipient's feelings.",
            _ => "Unable to determine politeness level"
        };
    }

    public void Dispose()
    {
        _inferenceSession?.Dispose();
    }
}
