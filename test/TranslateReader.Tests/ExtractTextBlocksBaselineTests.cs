using TranslateReader.Business.Engines;
using TranslateReader.Utilities;

namespace TranslateReader.Tests;

/// Characterization of the three real EPUB fixtures (D-2026-08-01-div-paragraph-translation-2).
/// The numbers below were MEASURED against the code as it stood before the disjoint-union
/// selection of D-...-7 landed. They exist so that widening the selection to leaf divs cannot
/// silently change what already works. Block count alone is not enough: a different text with the
/// same cardinality would slip through, so the non-space character sum is pinned as well.
/// Measured baseline: Wardley Maps 2124 blocks / 678242 chars, Righting software 1329 / 292254,
/// Practice Makes Perfect 6102 / 239075.
public class ExtractTextBlocksBaselineTests
{
    private static string FindEpub(string pattern)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestData");
        return Directory.GetFiles(dir, pattern).Single();
    }

    private static readonly string PracticeEpub = FindEpub("Practice Makes Perfect*.epub");
    private static readonly string RightingEpub = FindEpub("Righting software*.epub");
    private static readonly string WardleyEpub = FindEpub("Wardley Maps*.epub");

    private readonly ParsingEngine _sut = new();

    [Fact]
    public async Task WardleyMaps_ExtractTextBlocks_PreservesBaselineBlockCount()
    {
        var (blockCount, textCharCount) = await MeasureAsync(WardleyEpub);

        Assert.Equal(2124, blockCount);
        Assert.Equal(678242, textCharCount);
    }

    [Fact]
    public async Task RightingSoftware_ExtractTextBlocks_PreservesBaselineBlockCount()
    {
        var (blockCount, textCharCount) = await MeasureAsync(RightingEpub);

        Assert.Equal(1329, blockCount);
        Assert.Equal(292254, textCharCount);
    }

    [Fact]
    public async Task PracticeMakesPerfect_ExtractTextBlocks_PreservesBaselineBlockCount()
    {
        var (blockCount, textCharCount) = await MeasureAsync(PracticeEpub);

        Assert.Equal(6102, blockCount);
        Assert.Equal(239075, textCharCount);
    }

    private async Task<(int BlockCount, int TextCharCount)> MeasureAsync(string epubPath)
    {
        var chapters = await _sut.ExtractChaptersAsync(epubPath);
        var blockCount = 0;
        var textCharCount = 0;

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(epubPath, chapter.HRef, string.Empty);
            foreach (var block in HtmlUtility.ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html)))
            {
                blockCount++;
                textCharCount += block.Count(c => !char.IsWhiteSpace(c));
            }
        }

        return (blockCount, textCharCount);
    }
}
