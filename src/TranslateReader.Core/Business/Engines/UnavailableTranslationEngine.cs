using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

namespace TranslateReader.Business.Engines;

/// <summary>
/// Null-object <see cref="ITranslationEngine"/> for platforms that ship no native inference
/// backend (iOS/MacCatalyst today). Every member throws <see cref="TranslationUnavailableException"/>
/// instead of ever touching LLamaSharp, whose native loader throws
/// <see cref="PlatformNotSupportedException"/> from its own type initializer on these platforms
/// (D-2026-08-16-llm-mobile-5). Registering this in place of the real engine is what removes that
/// crash entirely: the type is never touched, so the failing initializer never runs.
/// </summary>
public sealed class UnavailableTranslationEngine : ITranslationEngine
{
    private const string UnavailableMessage =
        "Translation is not available on this platform: no native backend has shipped for it yet.";

    public bool IsReady => false;

    public Task InitializeAsync(string modelPath, CancellationToken ct) =>
        throw new TranslationUnavailableException(UnavailableMessage);

    public IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemMessage, string userMessage, float temperature, int maxTokens, CancellationToken ct) =>
        throw new TranslationUnavailableException(UnavailableMessage);

    public Task<string> GenerateAsync(
        string systemMessage, string userMessage, float temperature, int maxTokens, CancellationToken ct) =>
        throw new TranslationUnavailableException(UnavailableMessage);

    public void Dispose() => GC.SuppressFinalize(this);
}
