using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Microsoft.Windows.AI.MachineLearning;
using System.Diagnostics;
using System.Numerics.Tensors;
using System.Text.RegularExpressions;
using Tensor = System.Numerics.Tensors.Tensor;
using Contoso.AI.TextEmbedder.MiniLML6;

// 'System.Numerics.Tensors' is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable SYSLIB5001

namespace Contoso.AI;

/// <summary>
/// Provides text embedding capabilities using the all-MiniLM-L6-v2 model.
/// Generates 384-dimensional embeddings for semantic search and similarity tasks.
/// </summary>
public sealed partial class TextEmbedderMiniLML6 : ITextEmbedder
{
    private readonly OrtEnv _env;
    private readonly SessionOptions _sessionOptions;
    private readonly InferenceSession _inferenceSession;
    private readonly BertTokenizer _tokenizer;
    private bool _disposed;

    [GeneratedRegex(@"[\u0000-\u001F\u007F-\uFFFF]")]
    private static partial Regex ControlCharRegex();

    private const string ModelPath = "Models/MiniLM-L6-v2/model.onnx";
    private const string VocabPath = "Models/MiniLM-L6-v2/vocab.txt";
    private const int EmbeddingDimensions = 384;

    /// <summary>
    /// Private constructor - use <see cref="CreateAsync"/> factory method.
    /// </summary>
    private TextEmbedderMiniLML6(OrtEnv env, SessionOptions sessionOptions, InferenceSession inferenceSession, BertTokenizer tokenizer)
    {
        _env = env;
        _sessionOptions = sessionOptions;
        _inferenceSession = inferenceSession;
        _tokenizer = tokenizer;
    }

    /// <summary>
    /// Gets the ready state of the text embedder feature.
    /// </summary>
    /// <returns>The ready state indicating if the feature can be used.</returns>
    public static AIFeatureReadyState GetReadyState()
    {
        try
        {
            // Check if model files exist
            if (!File.Exists(ModelPath) || !File.Exists(VocabPath))
            {
                Debug.WriteLine($"[TextEmbedderMiniLML6] Model files not found");
                return AIFeatureReadyState.NotReady;
            }

            return AIFeatureReadyState.Ready;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TextEmbedderMiniLML6] Error checking ready state: {ex.Message}");
            return AIFeatureReadyState.NotReady;
        }
    }

    /// <summary>
    /// Ensures the text embedder feature is ready by checking for necessary dependencies.
    /// </summary>
    /// <returns>A task containing the preparation result.</returns>
    public static async Task<AIFeatureReadyResult> EnsureReadyAsync()
    {
        try
        {
            // Check if model files exist
            if (!File.Exists(ModelPath) || !File.Exists(VocabPath))
            {
                throw new FileNotFoundException($"Model files not found: {ModelPath} or {VocabPath}");
            }

            // Get the Windows ML EP catalog
            var catalog = ExecutionProviderCatalog.GetDefault();

            try
            {
                // Try to register execution providers (optional)
                await catalog.RegisterCertifiedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TextEmbedderMiniLML6] Note: Could not register execution providers: {ex.Message}");
                // Not critical - can still run on CPU
            }

            Debug.WriteLine("[TextEmbedderMiniLML6] Text embedder feature is ready");
            return AIFeatureReadyResult.Success();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TextEmbedderMiniLML6] Failed to ensure ready: {ex.Message}");
            return AIFeatureReadyResult.Failed(ex);
        }
    }

    /// <summary>
    /// Creates a new text embedder instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the initialized embedder.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the model files are not found.</exception>
    public static async Task<ITextEmbedder> CreateAsync()
    {
        if (!File.Exists(ModelPath) || !File.Exists(VocabPath))
        {
            throw new FileNotFoundException($"Model files not found: {ModelPath} or {VocabPath}");
        }

        var env = OrtEnv.Instance();

        var sessionOptions = new SessionOptions();
        sessionOptions.RegisterOrtExtensions();

        // Try to get optimal execution provider
        var catalog = ExecutionProviderCatalog.GetDefault();
        try
        {
            await catalog.EnsureAndRegisterCertifiedAsync();

            // Try GPU first, fall back to CPU
            var providers = catalog.FindAllProviders();
            var dmlProvider = providers.FirstOrDefault(p => p.Name == "DmlExecutionProvider");
            
            if (dmlProvider != null && dmlProvider.ReadyState != ExecutionProviderReadyState.NotPresent)
            {
                sessionOptions.AppendExecutionProvider_DML();
                Debug.WriteLine("[TextEmbedderMiniLML6] Using DirectML (GPU) execution provider");
            }
            else
            {
                Debug.WriteLine("[TextEmbedderMiniLML6] Using CPU execution provider");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TextEmbedderMiniLML6] Could not configure execution provider: {ex.Message}, using CPU");
        }

        var inferenceSession = new InferenceSession(ModelPath, sessionOptions);
        var tokenizer = BertTokenizer.Create(VocabPath);

        return new TextEmbedderMiniLML6(env, sessionOptions, inferenceSession, tokenizer);
    }

    /// <summary>
    /// Generates embeddings for the provided texts.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <returns>An array of embeddings, one for each input text.</returns>
    public Embedding[] GenerateEmbeddings(params string[] texts)
    {
        return GenerateEmbeddingsAsync(texts).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Generates embeddings for the provided texts asynchronously.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing an array of embeddings, one for each input text.</returns>
    public async Task<Embedding[]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TextEmbedderMiniLML6));

        var textList = texts.ToList();
        if (textList.Count == 0)
            return Array.Empty<Embedding>();

        return await Task.Run(() => InternalGenerateEmbeddings(textList), cancellationToken);
    }

    private Embedding[] InternalGenerateEmbeddings(IEnumerable<string> texts)
    {
        try
        {
            // Clean texts by removing control characters
            var cleanedTexts = texts.Select(s => ControlCharRegex().Replace(s, string.Empty)).ToList();
            
            // Tokenize texts
            var encoded = _tokenizer.EncodeBatch(cleanedTexts);
            var count = cleanedTexts.Count;

            var input = new TextEmbedder.MiniLML6.EmbeddingModelInput
            {
                InputIds = encoded.SelectMany(t => t.InputIds).ToArray(),
                AttentionMask = encoded.SelectMany(t => t.AttentionMask).ToArray(),
                TokenTypeIds = encoded.SelectMany(t => t.TokenTypeIds).ToArray()
            };

            int sequenceLength = input.InputIds.Length / count;

            // Create input tensors
            using var inputIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
                input.InputIds,
                [count, sequenceLength]);

            using var attMaskOrtValue = OrtValue.CreateTensorValueFromMemory(
                input.AttentionMask,
                [count, sequenceLength]);

            using var typeIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
                input.TokenTypeIds,
                [count, sequenceLength]);

            var inputNames = new List<string>
            {
                "input_ids",
                "attention_mask",
                "token_type_ids"
            };

            var inputs = new List<OrtValue>
            {
                inputIdsOrtValue,
                attMaskOrtValue,
                typeIdsOrtValue
            };

            using var output = OrtValue.CreateAllocatedTensorValue(
                OrtAllocator.DefaultInstance, 
                TensorElementType.Float, 
                [count, sequenceLength, EmbeddingDimensions]);

            // Run inference
            using var runOptions = new RunOptions();
            _inferenceSession.Run(runOptions, inputNames, inputs, _inferenceSession.OutputNames, [output]);

            var typeAndShape = output.GetTensorTypeAndShape();

            // Apply mean pooling
            ReadOnlyTensorSpan<float> sentenceEmbeddings = MeanPooling(
                output.GetTensorDataAsSpan<float>(), 
                input.AttentionMask, 
                typeAndShape.Shape);

            // Normalize embeddings
            float[] resultArray = NormalizeSentenceEmbeddings(sentenceEmbeddings, typeAndShape.Shape);

            // Split into individual embeddings
            var embeddingVectors = Enumerable.Chunk(resultArray, resultArray.Length / count).ToArray();
            return embeddingVectors.Select(v => new Embedding(v.ToArray())).ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TextEmbedderMiniLML6] Embedding generation failed: {ex}");
            throw;
        }
    }

    private static ReadOnlyTensorSpan<float> MeanPooling(ReadOnlySpan<float> embeddings, long[] attentionMask, long[] shape)
    {
        // Extract shapes
        var batchSize = (int)shape[0];
        var sequenceLength = (int)shape[1];
        var embeddingSize = (int)shape[2];

        // Create output tensor for mean pooled embeddings
        float[] result = new float[batchSize * embeddingSize];

        for (int batch = 0; batch < batchSize; batch++)
        {
            float[] sumEmbedding = new float[embeddingSize];
            float sumMask = 0;

            for (int seq = 0; seq < sequenceLength; seq++)
            {
                int maskIdx = batch * sequenceLength + seq;
                float mask = attentionMask[maskIdx];
                
                if (mask > 0)
                {
                    int embeddingOffset = (batch * sequenceLength + seq) * embeddingSize;
                    
                    for (int i = 0; i < embeddingSize; i++)
                    {
                        sumEmbedding[i] += embeddings[embeddingOffset + i] * mask;
                    }
                    
                    sumMask += mask;
                }
            }

            // Calculate mean
            int resultOffset = batch * embeddingSize;
            for (int i = 0; i < embeddingSize; i++)
            {
                result[resultOffset + i] = sumMask > 0 ? sumEmbedding[i] / sumMask : 0;
            }
        }

        return new ReadOnlyTensorSpan<float>(result, [batchSize, embeddingSize], []);
    }

    private static float[] NormalizeSentenceEmbeddings(ReadOnlyTensorSpan<float> sentenceEmbeddings, long[] shape)
    {
        int batchSize = (int)shape[0];
        int embeddingSize = (int)shape[2];

        float[] result = new float[batchSize * embeddingSize];
        
        // Convert to array for easier indexing
        float[] embeddingsArray = [.. sentenceEmbeddings];

        for (int batch = 0; batch < batchSize; batch++)
        {
            // Calculate L2 norm
            float sumSquared = 0;
            int offset = batch * embeddingSize;
            
            for (int i = 0; i < embeddingSize; i++)
            {
                float val = embeddingsArray[offset + i];
                sumSquared += val * val;
            }

            // Normalize
            float norm = MathF.Sqrt(Math.Max(sumSquared, 1e-9f));
            
            for (int i = 0; i < embeddingSize; i++)
            {
                result[offset + i] = embeddingsArray[offset + i] / norm;
            }
        }

        return result;
    }

    /// <summary>
    /// Disposes of the resources used by the text embedder.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _inferenceSession?.Dispose();
            _sessionOptions?.Dispose();
            _disposed = true;
        }
    }
}
