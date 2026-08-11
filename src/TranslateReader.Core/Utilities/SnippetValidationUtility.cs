using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace TranslateReader.Utilities;

/// <summary>
/// Pure, stateless checks that decide whether a snippet translation response is plausible enough to
/// persist or surface to the reader, independent of how it was produced (fresh inference, a cache
/// hit, or a row already sitting in <c>SnippetTranslations</c>).
/// </summary>
public static partial class SnippetValidationUtility
{
    private const int RegexTimeoutMilliseconds = 1000;

    // EN->PT rarely expands past ~1.6x; 3x the excerpt's own length plus slack for very short
    // excerpts is a conservative ceiling that still catches a model echoing the whole surrounding
    // paragraph (or a stale, pre-hardening cache/DB row) back instead of just the delimited excerpt.
    private const int LengthRatioMultiplier = 3;
    private const int LengthRatioSlack = 120;

    // A refusal always opens the response ("No, I cannot...", "Desculpe, ..."), so only the opening
    // window is checked - a legitimate translation that merely discusses apologies or AI further in
    // is never flagged.
    private const int RefusalWindowChars = 80;

    private static readonly string[] RefusalPhrases =
    [
        "i cannot", "i can't", "i'm sorry", "i am sorry", "as an ai",
        "não posso", "desculpe, ", "lo siento"
    ];

    private const int MinLengthForLanguageCheck = 40;
    private const int MinStopwordHits = 2;
    private const double MinStopwordRatio = 0.08;

    // Minimal, hand-picked stopword sets for the languages the settings UI actually offers
    // (SettingsOverlay/TranslateBookPopup). A real translation of >= 40 chars into one of these
    // languages is overwhelmingly likely to contain several of its most common function words; a
    // response that fails this ratio is far more likely to be an English refusal, an echo of the
    // source, or another language entirely than a genuine, on-target translation.
    private static readonly FrozenDictionary<string, FrozenSet<string>> TargetLanguageStopwords =
        new Dictionary<string, FrozenSet<string>>(StringComparer.Ordinal)
        {
            ["Brazilian Portuguese (PT-BR)"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "de", "que", "não", "uma", "um", "para", "com", "os", "as", "do", "da", "em", "é",
                "se", "mais", "como", "mas", "foi", "ser", "são", "por", "isso", "ele", "ela"
            }.ToFrozenSet(StringComparer.Ordinal),
            ["English"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "the", "of", "and", "to", "in", "is", "that", "it", "for", "on", "with", "as",
                "this", "are", "be"
            }.ToFrozenSet(StringComparer.Ordinal),
            ["Spanish"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "de", "que", "no", "una", "un", "para", "con", "los", "las", "del", "en", "es",
                "se", "más", "como", "pero"
            }.ToFrozenSet(StringComparer.Ordinal)
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// True when a snippet translation response is plausible enough to trust: not implausibly
    /// longer than the excerpt it translates (when the excerpt is known), does not open with a
    /// model refusal, and - when long enough and a stopword table exists for the target language -
    /// contains a plausible share of that language's most common words.
    /// <paramref name="originalText"/> is null at load-time purge, where only the persisted
    /// translation (never the original excerpt) is available; the length-ratio check is skipped in
    /// that case and the other two still apply.
    /// </summary>
    public static bool IsPlausibleSnippetTranslation(
        string? originalText, string translated, string sourceLanguage, string targetLanguage)
    {
        if (originalText is not null && IsImplausiblyLong(originalText, translated))
            return false;

        return !ContainsRefusalOpening(translated) &&
            HasPlausibleTargetLanguageRatio(translated, sourceLanguage, targetLanguage);
    }

    private static bool IsImplausiblyLong(string text, string translated) =>
        translated.Length > (text.Length * LengthRatioMultiplier) + LengthRatioSlack;

    private static bool ContainsRefusalOpening(string translated)
    {
        var windowLength = Math.Min(RefusalWindowChars, translated.Length);
        var window = translated.AsSpan(0, windowLength).Trim();
        foreach (var phrase in RefusalPhrases)
        {
            if (window.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static bool HasPlausibleTargetLanguageRatio(
        string translated, string sourceLanguage, string targetLanguage)
    {
        if (translated.Length < MinLengthForLanguageCheck) return true;
        if (string.Equals(sourceLanguage, targetLanguage, StringComparison.Ordinal)) return true;
        if (!TargetLanguageStopwords.TryGetValue(targetLanguage, out var stopwords)) return true;

        var tokenCount = 0;
        var hits = 0;
        foreach (var word in NonLetterRegex().Split(translated))
        {
            if (word.Length == 0) continue;
            tokenCount++;
            if (stopwords.Contains(word.ToLowerInvariant()))
                hits++;
        }

        if (tokenCount == 0) return true;
        return hits >= Math.Max(MinStopwordHits, tokenCount * MinStopwordRatio);
    }

    [GeneratedRegex(@"[^\p{L}]+", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex NonLetterRegex();
}
