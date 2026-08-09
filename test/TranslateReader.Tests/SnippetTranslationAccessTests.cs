using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class SnippetTranslationAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private SnippetTranslationAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private static SnippetTranslation MakeSnippet(
        int bookId = 1, string chapterHRef = "cap1.html", int paragraphIndex = 0,
        int sentenceStart = 0, int sentenceEnd = 0, string originalHash = "abcd1234",
        string translatedText = "Texto traduzido", bool showingOriginal = false) =>
        new(0, bookId, chapterHRef, paragraphIndex, sentenceStart, sentenceEnd, originalHash,
            translatedText, showingOriginal, DateTime.UtcNow);

    [Fact]
    public async Task SaveAndFetch_RoundTrip()
    {
        var sut = CreateSut();

        await sut.SaveSnippetAsync(MakeSnippet());
        var result = await sut.FetchSnippetsAsync(1, "cap1.html");

        Assert.Single(result);
        Assert.Equal("Texto traduzido", result[0].TranslatedText);
        Assert.Equal("abcd1234", result[0].OriginalHash);
    }

    [Fact]
    public async Task FetchSnippetsAsync_FiltersByChapter()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(chapterHRef: "cap1.html"));
        await sut.SaveSnippetAsync(MakeSnippet(chapterHRef: "cap2.html"));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");

        Assert.Single(result);
        Assert.Equal("cap1.html", result[0].ChapterHRef);
    }

    [Fact]
    public async Task FetchSnippetsAsync_ReturnsEmptyListWhenNoneSaved()
    {
        var result = await CreateSut().FetchSnippetsAsync(1, "cap1.html");

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchSnippetsAsync_OrdersByParagraphThenSentenceStart()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(paragraphIndex: 1, sentenceStart: 0, sentenceEnd: 0));
        await sut.SaveSnippetAsync(MakeSnippet(paragraphIndex: 0, sentenceStart: 2, sentenceEnd: 2));
        await sut.SaveSnippetAsync(MakeSnippet(paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 0));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");

        Assert.Equal(3, result.Count);
        Assert.Equal((0, 0), (result[0].ParagraphIndex, result[0].SentenceStart));
        Assert.Equal((0, 2), (result[1].ParagraphIndex, result[1].SentenceStart));
        Assert.Equal((1, 0), (result[2].ParagraphIndex, result[2].SentenceStart));
    }

    [Fact]
    public async Task SaveSnippetAsync_OnSameAnchor_Updates()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(translatedText: "Primeira versao"));

        await sut.SaveSnippetAsync(MakeSnippet(translatedText: "Segunda versao"));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.Single(result);
        Assert.Equal("Segunda versao", result[0].TranslatedText);
    }

    [Fact]
    public async Task SaveSnippetAsync_DeletesOverlappingRangeInSameParagraph()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 0, sentenceEnd: 1, translatedText: "A"));

        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 1, sentenceEnd: 2, translatedText: "B"));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.Single(result);
        Assert.Equal((1, 2), (result[0].SentenceStart, result[0].SentenceEnd));
        Assert.Equal("B", result[0].TranslatedText);
    }

    [Fact]
    public async Task SaveSnippetAsync_KeepsNonOverlappingRangeInSameParagraph()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 0, sentenceEnd: 0, translatedText: "A"));

        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 2, sentenceEnd: 3, translatedText: "B"));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SaveSnippetAsync_KeepsOverlappingRangeInAnotherParagraph()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(paragraphIndex: 0, sentenceStart: 0, sentenceEnd: 1));

        await sut.SaveSnippetAsync(MakeSnippet(paragraphIndex: 1, sentenceStart: 0, sentenceEnd: 1));

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SetShowingOriginalAsync_FlipsTheFlag()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(showingOriginal: false));

        await sut.SetShowingOriginalAsync(1, "cap1.html", 0, 0, 0, showingOriginal: true);

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.True(result[0].ShowingOriginal);
    }

    [Fact]
    public async Task RemoveSnippetAsync_RemovesOnlyTheTargetAnchor()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 0, sentenceEnd: 0));
        await sut.SaveSnippetAsync(MakeSnippet(sentenceStart: 5, sentenceEnd: 5));

        await sut.RemoveSnippetAsync(1, "cap1.html", 0, 0, 0);

        var result = await sut.FetchSnippetsAsync(1, "cap1.html");
        Assert.Single(result);
        Assert.Equal(5, result[0].SentenceStart);
    }

    [Fact]
    public async Task RemoveSnippetsForBookAsync_DoesNotTouchAnotherBook()
    {
        var sut = CreateSut();
        await sut.SaveSnippetAsync(MakeSnippet(bookId: 1));
        await sut.SaveSnippetAsync(MakeSnippet(bookId: 2));

        await sut.RemoveSnippetsForBookAsync(1);

        Assert.Empty(await sut.FetchSnippetsAsync(1, "cap1.html"));
        Assert.Single(await sut.FetchSnippetsAsync(2, "cap1.html"));
    }
}
