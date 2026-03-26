namespace TranslateReader.Contracts.Engines;

public interface ITranslationEngine : IDisposable
{
    Task InitializeAsync(string modelPath, CancellationToken ct);
    bool IsReady { get; }
    IAsyncEnumerable<string> GenerateStreamingAsync(string systemMessage, string userMessage, float temperature, int maxTokens, CancellationToken ct);
    Task<string> GenerateAsync(string systemMessage, string userMessage, float temperature, int maxTokens, CancellationToken ct);
}
