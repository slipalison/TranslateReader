using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

[CollectionDefinition("NonParallel", DisableParallelization = true)]
public class NonParallelCollection;

// Measures the property the phase is about: extracting the 229 images of the large fixture (44 MB
// in total) must not hold the whole book in memory. GC.GetTotalMemory reports the PROCESS, so this
// class owns a non-parallel collection - another test parsing a fixture at the same time would
// poison the sample. There is deliberately NO timing assertion (D-2026-07-31-conversion-performance-1):
// a wall clock in CI is flaky, a retained-bytes ceiling is not. Real files under TestData are the
// named exception to .claude/rules/csharp.md section 6 already used by ParsingEngineTests.
[Collection("NonParallel")]
public class ParsingEngineMemoryTests
{
    private const long MaxRetainedBytes = 20L * 1024 * 1024;
    private const int SampleEveryNImages = 32;

    private static string FindEpub(string pattern)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestData");
        return Directory.GetFiles(dir, pattern).Single();
    }

    private static readonly string PracticeEpub = FindEpub("Practice Makes Perfect*.epub");
    private static readonly string WardleyEpub = FindEpub("Wardley Maps*.epub");

    private readonly ParsingEngine _sut = new();

    [Fact]
    [Trait("Category", "Slow")]
    public async Task ExtractAllImagesAsync_OverTheLargeFixture_KeepsThePeakRetainedMemoryBounded()
    {
        await WarmUpAsync();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var baselineBytes = GC.GetTotalMemory(forceFullCollection: true);

        var count = 0;
        long totalBytes = 0;
        var peakBytes = baselineBytes;

        await foreach (var image in _sut.ExtractAllImagesAsync(WardleyEpub))
        {
            count++;
            totalBytes += image.Content.Length;

            // Sampling with a forced full collection leaves only what is still REACHABLE, so a
            // peak here means the implementation is retaining images, not merely producing garbage.
            if (count % SampleEveryNImages == 0)
                peakBytes = Math.Max(peakBytes, GC.GetTotalMemory(forceFullCollection: true));
        }

        var peakRetainedDelta = peakBytes - baselineBytes;

        Assert.True(count >= 200, $"Esperado >= 200 imagens no fixture grande, obtido {count}.");
        Assert.True(totalBytes > 35L * 1024 * 1024,
            $"Esperado > 35 MB de bytes de imagem lidos, obtido {totalBytes / 1024 / 1024} MB.");
        Assert.True(peakRetainedDelta < MaxRetainedBytes,
            $"Pico de memoria retida durante a extracao: {peakRetainedDelta / 1024 / 1024} MB " +
            $"(teto {MaxRetainedBytes / 1024 / 1024} MB). O caminho eager retinha ~44 MB.");
    }

    // JIT, statics and the library's internal buffers are charged to whoever enumerates first;
    // paying that on the short fixture keeps it out of the large fixture's baseline.
    private async Task WarmUpAsync()
    {
        await foreach (var image in _sut.ExtractAllImagesAsync(PracticeEpub))
            _ = image.Content.Length;
    }
}
