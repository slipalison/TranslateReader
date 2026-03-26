using System.Runtime.CompilerServices;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using TranslateReader.Contracts.Engines;

namespace TranslateReader.Business.Engines;

public class TranslationEngine : ITranslationEngine
{
    private LLamaWeights? _weights;
    private ModelParams? _modelParams;
    private bool _disposed;

    public bool IsReady => _weights is not null && !_disposed;

    public Task InitializeAsync(string modelPath, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsReady)
            return Task.CompletedTask;

        _modelParams = CreateModelParams(modelPath);
        _weights = LLamaWeights.LoadFromFile(_modelParams);

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemMessage,
        string userMessage,
        float temperature,
        int maxTokens,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var executor = CreateExecutor(systemMessage);
        var inferenceParams = CreateInferenceParams(temperature, maxTokens);

        await foreach (var token in executor.InferAsync(userMessage, inferenceParams, ct))
        {
            yield return token;
        }
    }

    public async Task<string> GenerateAsync(
        string systemMessage,
        string userMessage,
        float temperature,
        int maxTokens,
        CancellationToken ct)
    {
        var result = new StringBuilder();

        await foreach (var token in GenerateStreamingAsync(systemMessage, userMessage, temperature, maxTokens, ct))
        {
            result.Append(token);
        }

        return result.ToString();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _weights?.Dispose();
        _weights = null;
        _modelParams = null;
    }

    private StatelessExecutor CreateExecutor(string systemMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var weights = _weights ?? throw new InvalidOperationException("Engine not initialized. Call InitializeAsync first.");
        return new StatelessExecutor(weights, _modelParams!)
        {
            ApplyTemplate = true,
            SystemMessage = systemMessage
        };
    }

    private static ModelParams CreateModelParams(string modelPath)
    {
        return new ModelParams(modelPath)
        {
            ContextSize = 2048,
            GpuLayerCount = 0,
            UseMemorymap = true,
            BatchSize = 512,
            Threads = CalculateThreadCount()
        };
    }

    private static InferenceParams CreateInferenceParams(float temperature, int maxTokens)
    {
        return new InferenceParams
        {
            MaxTokens = maxTokens,
            AntiPrompts = ["\n\n\n"],
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = temperature
            }
        };
    }

    private static int CalculateThreadCount()
    {
        var cores = Environment.ProcessorCount;
        var isMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        return isMobile ? Math.Max(1, cores / 2) : Math.Max(1, cores - 2);
    }
}
