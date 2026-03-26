using TranslateReader.Contracts.Access;

namespace TranslateReader.Access;

public class ModelAccess(HttpClient httpClient, string modelsDirectory) : IModelAccess
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _modelsDirectory = modelsDirectory;

    private const int BufferSize = 81_920;
    private const double MinProgressIncrement = 0.005;

    public async Task DownloadModelAsync(string url, IProgress<double>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(_modelsDirectory);

        var fileName = GetFileNameFromUrl(url);
        var finalPath = Path.Combine(_modelsDirectory, fileName);
        var tmpPath = finalPath + ".tmp";

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        long bytesRead = 0;
        double lastReportedProgress = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

        var buffer = new byte[BufferSize];
        int read;
        while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            bytesRead += read;

            if (totalBytes > 0)
            {
                var currentProgress = (double)bytesRead / totalBytes.Value;
                if (currentProgress - lastReportedProgress >= MinProgressIncrement || currentProgress >= 1.0)
                {
                    lastReportedProgress = currentProgress;
                    progress?.Report(currentProgress);
                }
            }
        }

        fileStream.Close();
        File.Move(tmpPath, finalPath, overwrite: true);
    }

    public bool IsModelAvailable()
    {
        if (!Directory.Exists(_modelsDirectory))
            return false;

        return Directory.EnumerateFiles(_modelsDirectory, "*.gguf").Any();
    }

    public string GetModelPath()
    {
        if (!Directory.Exists(_modelsDirectory))
            throw new FileNotFoundException("Models directory does not exist.");

        var modelFile = Directory.EnumerateFiles(_modelsDirectory, "*.gguf").FirstOrDefault();
        return modelFile ?? throw new FileNotFoundException("No model file found.");
    }

    public Task DeleteModelAsync()
    {
        if (!Directory.Exists(_modelsDirectory))
            return Task.CompletedTask;

        foreach (var file in Directory.EnumerateFiles(_modelsDirectory))
            File.Delete(file);

        return Task.CompletedTask;
    }

    private static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        return Path.GetFileName(uri.LocalPath);
    }
}
