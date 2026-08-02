namespace TranslateReader.Contracts.Access;

public interface IModelAccess
{
    Task DownloadModelAsync(string url, IProgress<double>? progress, CancellationToken ct);

    /// <summary>Checks whether the exact model file named <paramref name="fileName"/> exists on disk.</summary>
    bool IsModelAvailable(string fileName);

    /// <summary>Returns the full path to the model file named <paramref name="fileName"/>, if it exists.</summary>
    string GetModelPath(string fileName);
    Task DeleteModelAsync();
}
