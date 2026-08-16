namespace TranslateReader.Models;

public sealed record SnippetRequest(
    string ChapterHRef,
    int ParagraphIndex,
    int SentenceStart,
    int SentenceEnd,
    string Text,
    string Paragraph);
