using System.Runtime.CompilerServices;
using System.Text;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;

namespace TranslateReader.Business.Engines;

/// <summary>
/// iOS's <see cref="ITranslationEngine"/> (D-2026-08-16-llm-mobile-5): LLamaSharp's native loader
/// crashes from its own type initializer on iOS, so this engine talks to llama.cpp through its own
/// thin P/Invoke boundary (<see cref="ILlamaNativeAccess"/>) instead. This class owns the actual
/// generation loop -- tokenize, decode, sample, detokenize, repeat until end-of-generation or
/// <paramref name="ct"/> is cancelled -- and is where the real logic lives: it compiles and is
/// unit-tested in plain <c>net10.0</c>, with no device and no GGUF file required. Only the
/// declarations it calls through <see cref="ILlamaNativeAccess"/> are platform-specific.
/// </summary>
public sealed class LlamaCppTranslationEngine(ILlamaNativeAccess nativeAccess) : ITranslationEngine
{
    private const int ContextSize = 2048;

    private bool _modelLoaded;
    private bool _disposed;

    public bool IsReady => _modelLoaded && !_disposed;

    public Task InitializeAsync(string modelPath, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ct.ThrowIfCancellationRequested();

        if (IsReady)
            return Task.CompletedTask;

        nativeAccess.LoadModel(modelPath);
        nativeAccess.CreateContext(ContextSize);
        _modelLoaded = true;

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemMessage,
        string userMessage,
        float temperature,
        int maxTokens,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsReady)
            throw new InvalidOperationException("Engine not initialized. Call InitializeAsync first.");

        // Fresh KV cache per generation: without this, a second TranslateSnippetAsync call would
        // decode its prompt on top of the previous generation's leftover context state.
        nativeAccess.ResetContext();

        var promptTokens = nativeAccess.Tokenize($"{systemMessage}\n{userMessage}");
        nativeAccess.Decode(promptTokens);

        for (var i = 0; i < maxTokens; i++)
        {
            ct.ThrowIfCancellationRequested();

            var token = nativeAccess.SampleNextToken(temperature);
            if (nativeAccess.IsEndOfGeneration(token))
                yield break;

            yield return nativeAccess.TokenToText(token);

            nativeAccess.Decode([token]);

            // WHY Task.Yield: keeps this a genuine suspension point per token, so a cancellation
            // requested while a consumer is between tokens is observed on the next loop iteration
            // instead of only at the boundaries of a single synchronous burst.
            await Task.Yield();
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

        await foreach (var piece in GenerateStreamingAsync(systemMessage, userMessage, temperature, maxTokens, ct))
        {
            result.Append(piece);
        }

        return result.ToString();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_disposed)
            return;

        _disposed = true;
        if (_modelLoaded)
        {
            nativeAccess.FreeContext();
            nativeAccess.FreeModel();
            _modelLoaded = false;
        }
    }
}
