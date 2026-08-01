namespace TranslateReader.Models;

/// <summary>
/// Outcome of a full-book translation: where the translated EPUB was written and how much of the
/// source text the block selection actually reached (1.0 = every non-space character of every
/// chapter body ended up inside a translated block).
/// </summary>
public record BookTranslationResult(string EpubPath, double CoveredTextRatio);
