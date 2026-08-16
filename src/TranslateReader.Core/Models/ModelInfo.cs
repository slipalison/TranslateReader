namespace TranslateReader.Models;

public record ModelInfo(
    string Name,
    string FileName,
    string DownloadUrl,
    long SizeBytes)
{
    /// <summary>
    /// Conservative estimate of process memory needed to load and run this model (weights + KV
    /// cache): 1.5x the GGUF file size. Compared against
    /// <see cref="Contracts.Utilities.IDeviceMemoryUtility"/> by
    /// <c>TranslationManager.InitializeEngineIfNeededAsync</c> before ever touching the engine
    /// (D-2026-08-16-llm-mobile-8) -- the threshold is data here, not a rule encoded in the Manager.
    /// </summary>
    public long RequiredMemoryBytes => SizeBytes + SizeBytes / 2;
}
