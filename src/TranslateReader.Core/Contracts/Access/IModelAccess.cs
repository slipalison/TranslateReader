namespace TranslateReader.Contracts.Access;

public interface IModelAccess
{
    Task DownloadModelAsync(string url, IProgress<double>? progress, CancellationToken ct);
    bool IsModelAvailable();
    string GetModelPath();
    Task DeleteModelAsync();
}
