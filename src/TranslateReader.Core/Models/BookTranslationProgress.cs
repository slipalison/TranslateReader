namespace TranslateReader.Models;

public record BookTranslationProgress(
    int CurrentChapter,
    int TotalChapters,
    int CurrentParagraph,
    int TotalParagraphs,
    double OverallProgress);
