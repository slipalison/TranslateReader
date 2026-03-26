using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class PromptUtilityTests
{
    private readonly PromptUtility _sut = new();

    [Fact]
    public void BuildTranslationMessages_ReturnsUserTextAsUserMessage()
    {
        var (_, userMessage) = _sut.BuildTranslationMessages("The cat sat on the mat", null, null, null);

        Assert.Equal("The cat sat on the mat", userMessage);
    }

    [Fact]
    public void BuildTranslationMessages_SystemMessageIncludesTranslationInstructions()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, null, null);

        Assert.Contains("Brazilian Portuguese", systemMessage);
        Assert.Contains("PT-BR", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_IncludesBookTitle_WhenProvided()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", "The Great Gatsby", null, null);

        Assert.Contains("Book: The Great Gatsby", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_IncludesChapterTitle_WhenProvided()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, "Chapter 1", null);

        Assert.Contains("Chapter: Chapter 1", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_IncludesPreviousParagraph_WhenProvided()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, null, "Previous text here");

        Assert.Contains("Previous text here", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_OmitsBookTitle_WhenNull()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, null, null);

        Assert.DoesNotContain("Book:", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_OmitsBookTitle_WhenEmpty()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", "", null, null);

        Assert.DoesNotContain("Book:", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_OmitsChapterTitle_WhenNull()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, null, null);

        Assert.DoesNotContain("Chapter:", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_OmitsPreviousParagraph_WhenWhitespace()
    {
        var (systemMessage, _) = _sut.BuildTranslationMessages("Hello", null, null, "   ");

        Assert.DoesNotContain("Previous paragraph", systemMessage);
    }

    [Fact]
    public void BuildTranslationMessages_IncludesAllContext_WhenAllProvided()
    {
        var (systemMessage, userMessage) = _sut.BuildTranslationMessages(
            "Hello world",
            "My Book",
            "Chapter 5",
            "Previous paragraph");

        Assert.Contains("Book: My Book", systemMessage);
        Assert.Contains("Chapter: Chapter 5", systemMessage);
        Assert.Contains("Previous paragraph", systemMessage);
        Assert.Equal("Hello world", userMessage);
    }
}
