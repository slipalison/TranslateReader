using System.Net;
using TranslateReader.Access;

namespace TranslateReader.Tests;

public class ModelAccessTests : IDisposable
{
    private const string ModelUrl = "https://models.invalid/gguf/test-model.gguf";
    private const string ModelFileName = "test-model.gguf";

    private readonly string _modelsDir;
    private readonly HttpClient _httpClient = new();
    private readonly List<IDisposable> _disposables = [];

    public ModelAccessTests()
    {
        _modelsDir = Path.Combine(Path.GetTempPath(), "TranslateReaderTests_" + Guid.NewGuid().ToString("N"));
    }

    private ModelAccess CreateSut() => new(_httpClient, _modelsDir);

    public void Dispose()
    {
        DisposeFixtures();
        GC.SuppressFinalize(this);
    }

    private void DisposeFixtures()
    {
        _httpClient.Dispose();
        foreach (var disposable in _disposables)
            disposable.Dispose();
        if (Directory.Exists(_modelsDir))
            Directory.Delete(_modelsDir, recursive: true);
    }

    [Fact]
    public void IsModelAvailable_ReturnsFalseWhenDirectoryDoesNotExist()
    {
        Assert.False(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void IsModelAvailable_ReturnsFalseWhenNoGgufFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "readme.txt"), "not a model");

        Assert.False(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void IsModelAvailable_ReturnsTrueWhenGgufExists()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf"), "fake model data");

        Assert.True(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void GetModelPath_ThrowsWhenDirectoryDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => CreateSut().GetModelPath());
    }

    [Fact]
    public void GetModelPath_ThrowsWhenNoGgufFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        Assert.Throws<FileNotFoundException>(() => CreateSut().GetModelPath());
    }

    [Fact]
    public void GetModelPath_ReturnsPathToGgufFile()
    {
        Directory.CreateDirectory(_modelsDir);
        var expectedPath = Path.Combine(_modelsDir, "test-model.gguf");
        File.WriteAllText(expectedPath, "fake model data");

        var result = CreateSut().GetModelPath();

        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public async Task DeleteModelAsync_RemovesAllFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf"), "data");
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf.tmp"), "partial");

        await CreateSut().DeleteModelAsync();

        Assert.Empty(Directory.EnumerateFiles(_modelsDir));
    }

    [Fact]
    public async Task DeleteModelAsync_DoesNothingWhenDirectoryMissing()
    {
        var exception = await Record.ExceptionAsync(() => CreateSut().DeleteModelAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DownloadModelAsync_CreatesTheDirectoryAndStoresTheModelUnderTheUrlFileName()
    {
        var payload = BuildPayload(200_000);
        var sut = CreateSut(payload, chunkSize: 81_920, declaredLength: payload.Length);

        await sut.DownloadModelAsync(ModelUrl, progress: null, CancellationToken.None);

        var finalPath = Path.Combine(_modelsDir, ModelFileName);
        Assert.True(Directory.Exists(_modelsDir));
        Assert.Equal(payload, await File.ReadAllBytesAsync(finalPath));
        Assert.False(File.Exists(finalPath + ".tmp"));
    }

    [Fact]
    public async Task DownloadModelAsync_OverwritesAModelAlreadyOnDisk()
    {
        Directory.CreateDirectory(_modelsDir);
        var finalPath = Path.Combine(_modelsDir, ModelFileName);
        await File.WriteAllBytesAsync(finalPath, [0, 0, 0]);
        var payload = BuildPayload(1_024);
        var sut = CreateSut(payload, chunkSize: 1_024, declaredLength: payload.Length);

        await sut.DownloadModelAsync(ModelUrl, progress: null, CancellationToken.None);

        Assert.Equal(payload, await File.ReadAllBytesAsync(finalPath));
    }

    [Fact]
    public async Task DownloadModelAsync_WithoutContentLength_DownloadsWithoutReportingProgress()
    {
        var payload = BuildPayload(4_096);
        var sut = CreateSut(payload, chunkSize: 1_024, declaredLength: null);
        var reports = new List<double>();

        await sut.DownloadModelAsync(ModelUrl, new Progress<double>(reports.Add), CancellationToken.None);

        Assert.Equal(payload, await File.ReadAllBytesAsync(Path.Combine(_modelsDir, ModelFileName)));
        Assert.Empty(reports);
    }

    [Fact]
    public async Task DownloadModelAsync_ReportsHalfPercentStepsAndAlwaysTheFinalCompletion()
    {
        var payload = BuildPayload(100_000);
        var sut = CreateSut(payload, chunkSize: 300, declaredLength: payload.Length);
        var reports = new List<double>();
        var progress = new SynchronousProgress(reports.Add);

        await sut.DownloadModelAsync(ModelUrl, progress, CancellationToken.None);

        Assert.Equal(1.0, reports[^1]);
        Assert.True(reports.Count < 334, $"expected throttled reporting, got {reports.Count}");
        for (var i = 1; i < reports.Count; i++)
            Assert.True(reports[i] > reports[i - 1], "progress must be monotonic");
    }

    [Fact]
    public async Task DownloadModelAsync_WhenProgressIncrementStaysBelowTheThreshold_ReportsNothing()
    {
        var payload = BuildPayload(1_000);
        var sut = CreateSut(payload, chunkSize: 100, declaredLength: 1_000_000);
        var reports = new List<double>();
        var progress = new SynchronousProgress(reports.Add);

        await sut.DownloadModelAsync(ModelUrl, progress, CancellationToken.None);

        Assert.Empty(reports);
    }

    [Fact]
    public async Task DownloadModelAsync_ThrowsAndWritesNothingWhenTheServerRejectsTheRequest()
    {
        var sut = CreateSut(BuildPayload(16), chunkSize: 16, declaredLength: 16, HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.DownloadModelAsync(ModelUrl, progress: null, CancellationToken.None));

        Assert.False(File.Exists(Path.Combine(_modelsDir, ModelFileName)));
    }

    [Fact]
    public async Task DownloadModelAsync_LetsCancellationPropagateAndLeavesNoFinalFile()
    {
        using var cts = new CancellationTokenSource();
        var payload = BuildPayload(4_096);
        var sut = CreateSut(payload, chunkSize: 512, declaredLength: payload.Length, afterRead: cts.Cancel);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.DownloadModelAsync(ModelUrl, progress: null, cts.Token));

        Assert.False(File.Exists(Path.Combine(_modelsDir, ModelFileName)));
    }

    private ModelAccess CreateSut(
        byte[] payload,
        int chunkSize,
        long? declaredLength,
        HttpStatusCode status = HttpStatusCode.OK,
        Action? afterRead = null)
    {
        var handler = new StubHttpMessageHandler(payload, chunkSize, declaredLength, status, afterRead);
        var client = new HttpClient(handler);
        _disposables.Add(client);
        _disposables.Add(handler);
        return new ModelAccess(client, _modelsDir);
    }

    private static byte[] BuildPayload(int size)
    {
        var payload = new byte[size];
        for (var i = 0; i < size; i++)
            payload[i] = (byte)(i % 251);
        return payload;
    }

    /// <summary>
    /// <see cref="Progress{T}"/> posts to the synchronization context, which would make the
    /// assertions racy; this variant invokes the callback inline.
    /// </summary>
    private sealed class SynchronousProgress(Action<double> onReport) : IProgress<double>
    {
        public void Report(double value) => onReport(value);
    }

    private sealed class StubHttpMessageHandler(
        byte[] payload,
        int chunkSize,
        long? declaredLength,
        HttpStatusCode status,
        Action? afterRead) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new StreamContent(new ChunkedStream(payload, chunkSize, afterRead));
            content.Headers.ContentLength = declaredLength;
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }
    }

    /// <summary>
    /// Non-seekable stream that hands out at most <paramref name="chunkSize"/> bytes per read, so a
    /// test can drive the download loop through a chosen number of iterations.
    /// </summary>
    private sealed class ChunkedStream(byte[] data, int chunkSize, Action? afterRead) : Stream
    {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            Fill(buffer.AsSpan(offset, count));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Fill(buffer.Span));
        }

        private int Fill(Span<byte> destination)
        {
            var remaining = data.Length - _position;
            if (remaining == 0)
                return 0;

            var count = Math.Min(Math.Min(chunkSize, destination.Length), remaining);
            data.AsSpan(_position, count).CopyTo(destination);
            _position += count;
            afterRead?.Invoke();
            return count;
        }
    }
}
