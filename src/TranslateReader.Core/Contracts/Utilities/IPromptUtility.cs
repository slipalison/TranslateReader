namespace TranslateReader.Contracts.Utilities;

public interface IPromptUtility
{
    (string SystemMessage, string UserMessage) BuildTranslationMessages(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle,
        string? previousParagraph);

    /// <summary>
    /// Builds the prompt for translating a sub-paragraph snippet: <paramref name="paragraph"/> is
    /// context only — the model must return the translation of <paramref name="snippet"/> alone.
    /// </summary>
    (string SystemMessage, string UserMessage) BuildSnippetTranslationMessages(
        string snippet,
        string paragraph,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle);

    /// <summary>
    /// Builds the prompt for translating a sub-paragraph snippet with NO surrounding paragraph
    /// context at all — the deterministic retry when a first attempt built with
    /// <see cref="BuildSnippetTranslationMessages(string, string, string, string, string?, string?)"/>
    /// came back implausibly long, since the paragraph is the only thing that can leak into the
    /// answer.
    /// </summary>
    (string SystemMessage, string UserMessage) BuildSnippetTranslationMessages(
        string snippet,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle);
}
