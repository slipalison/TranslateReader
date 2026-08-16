namespace TranslateReader.Models;

public sealed record SnippetTranslation(
    int Id,
    int BookId,
    string ChapterHRef,
    int ParagraphIndex,
    int SentenceStart,
    int SentenceEnd,
    string OriginalHash,
    string TranslatedText,
    bool ShowingOriginal,
    DateTime CreatedAt);
