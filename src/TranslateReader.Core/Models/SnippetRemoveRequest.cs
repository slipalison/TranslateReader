namespace TranslateReader.Models;

public sealed record SnippetRemoveRequest(
    string ChapterHRef,
    int ParagraphIndex,
    int SentenceStart,
    int SentenceEnd);
