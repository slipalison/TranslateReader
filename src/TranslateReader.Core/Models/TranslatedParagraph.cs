namespace TranslateReader.Models;

public record TranslatedParagraph(
    string Original,
    string Translated,
    int Index,
    int TotalParagraphs,
    double Progress);
