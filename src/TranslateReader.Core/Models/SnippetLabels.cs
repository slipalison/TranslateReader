namespace TranslateReader.Models;

/// <summary>
/// The full payload for window.setSnippetLabels: pt-BR UI strings, the active reading theme, and
/// the book's language pair, so the WebView never hardcodes a pt-BR string or a theme color.
/// </summary>
public sealed record SnippetLabels(
    string SelectHint,
    string ExtendTip,
    string SentenceOne,
    string SentenceMany,
    string TranslateSnip,
    string ExtendSel,
    string ShrinkSel,
    string OnlySentence,
    string ToggleSnip,
    string RemoveSnip,
    IReadOnlyDictionary<string, string> LangMap,
    SnippetTheme Theme,
    string SourceLanguage,
    string TargetLanguage);
