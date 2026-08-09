namespace TranslateReader.Models;

public sealed record SnippetToggleRequest(
    string ChapterHRef,
    int ParagraphIndex,
    int SentenceStart,
    int SentenceEnd,
    bool ShowingOriginal);
