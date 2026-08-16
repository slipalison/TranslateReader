using System.Runtime.CompilerServices;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

namespace TranslateReader.Business.Engines;

public sealed class TranslationEngine : ITranslationEngine
{
    private LLamaWeights? _weights;
    private ModelParams? _modelParams;
    private bool _disposed;
    private static bool _nativeLibraryConfigured;

    // Guards the one-time, expensive model load (csharp.md S3): without it, two callers racing
    // InitializeAsync on this singleton (e.g. a visible-paragraph translation and a background
    // book-translation job) would both observe IsReady == false and both call LoadFromFile,
    // doubling native memory use and leaking whichever LLamaWeights instance loses the race.
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public bool IsReady => _weights is not null && !_disposed;

    public async Task InitializeAsync(string modelPath, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsReady)
            return;

        await _initLock.WaitAsync(ct);
        try
        {
            // Re-check after acquiring the lock: the caller that won the race already finished
            // loading while this one was waiting, so this one must not load a second time.
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsReady)
                return;

            ConfigureNativeLibrary();
            _modelParams = CreateModelParams(modelPath);
            _weights = LLamaWeights.LoadFromFile(_modelParams);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static void ConfigureNativeLibrary()
    {
        if (_nativeLibraryConfigured)
            return;

        _nativeLibraryConfigured = true;

        ApplyNativeBackendPlan(NativeBackendPlan.For(DetectCurrentPlatform()));
    }

    private static TranslationPlatform DetectCurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return TranslationPlatform.Windows;
        if (OperatingSystem.IsAndroid()) return TranslationPlatform.Android;
        if (OperatingSystem.IsIOS()) return TranslationPlatform.IOS;
        if (OperatingSystem.IsMacCatalyst()) return TranslationPlatform.MacCatalyst;
        return TranslationPlatform.Other;
    }

    private static void ApplyNativeBackendPlan(NativeBackendPlan plan)
    {
        var config = NativeLibraryConfig.All
            .WithCuda(plan.UseCuda)
            .WithVulkan(plan.UseVulkan)
            .WithAutoFallback(plan.UseAutoFallback)
            .WithLogCallback((level, message) =>
                System.Diagnostics.Debug.WriteLine($"[LLamaSharp] {level}: {message}"));

        if (plan.SearchDirectory is not null)
        {
            config.WithSearchDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, plan.SearchDirectory));
        }
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
        GC.SuppressFinalize(this);
        if (_disposed)
            return;

        _disposed = true;
        _weights?.Dispose();
        _weights = null;
        _modelParams = null;

        _initLock.Dispose();
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
            GpuLayerCount = -1,
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
