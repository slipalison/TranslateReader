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
    // is never flagged. Widened from 80 (B-4): the meta-vocabulary co-occurrence check below needs
    // room for a phrase near the very start plus its qualifying context a little further out (the
    // screenshot refusal's "safety guidelines" sits past char 80).
    private const int RefusalWindowChars = 160;

    private static readonly string[] RefusalPhrases =
    [
        "i cannot", "i can't", "i'm sorry", "i am sorry", "as an ai",
        "não posso", "desculpe, ", "lo siento"
    ];

    // B-4: these SAME phrases open fiction dialogue ("\"I can't breathe,\" she whispered...",
    // "Desculpe, eu não quis te magoar...") at extremely high frequency - this IS the app's domain
    // (EPUB prose), not an edge case. A phrase alone is never enough; it only means "the model
    // refused" when the SAME opening window also talks about the act of translating itself. Matched
    // as whole words, never as a raw substring: "ai" as a substring collides with ordinary words
    // ("against", "explain", "maintain") almost as often as the phrases it exists to gate - exactly
    // the false-positive class this fix closes, just moved to a different trigger.
    private static readonly FrozenSet<string> RefusalMetaVocabulary =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "translation", "translate", "text", "content", "guidelines", "safety", "ai", "assist",
            "language", "apologize", "request", "provide",
            "tradução", "traduzir", "texto", "conteúdo", "diretrizes", "idioma", "solicitação",
            "fornecer", "traducción", "contenido"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

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
    /// True when a FRESH snippet translation response (a cache hit or a new inference result, where
    /// the source/target language pair is known and nothing has been persisted yet) is plausible
    /// enough to trust: not implausibly longer than the excerpt it translates (when the excerpt is
    /// known), does not open with a model refusal, and - when long enough and a stopword table
    /// exists for the target language - contains a plausible share of that language's most common
    /// words. <paramref name="originalText"/> may be null when the original excerpt is not available
    /// to the caller; the length-ratio check is skipped in that case and the other two still apply.
    /// Never use this overload to judge an already-persisted row at load time - see
    /// <see cref="IsPlausiblePersistedSnippetTranslation"/>.
    /// </summary>
    public static bool IsPlausibleSnippetTranslation(
        string? originalText, string translated, string sourceLanguage, string targetLanguage)
    {
        if (originalText is not null && IsImplausiblyLong(originalText, translated))
            return false;

        return !ContainsRefusalOpening(translated) &&
            HasPlausibleTargetLanguageRatio(translated, sourceLanguage, targetLanguage);
    }

    // At load-time purge (B-4) the row's own source/target language pair is NOT known - only the
    // persisted translation is, and the app's CURRENT settings can have changed since the row was
    // saved (e.g. target switched PT-BR -> Spanish). The stopword ratio would then judge a
    // perfectly legitimate PT-BR row against Spanish and delete it; the refusal blocklist is
    // language-agnostic and precise (phrase + meta-vocabulary co-occurrence), so it is the ONLY
    // check safe to run here. A refusal that slips through simply resurfaces next load (cheap); a
    // ratio false positive here would silently destroy a legitimate translation forever (expensive).
    public static bool IsPlausiblePersistedSnippetTranslation(string translated) =>
        !ContainsRefusalOpening(translated);

    private static bool IsImplausiblyLong(string text, string translated) =>
        translated.Length > (text.Length * LengthRatioMultiplier) + LengthRatioSlack;

    private static bool ContainsRefusalOpening(string translated)
    {
        var windowLength = Math.Min(RefusalWindowChars, translated.Length);
        var window = translated.AsSpan(0, windowLength).Trim();

        var hasRefusalPhrase = false;
        foreach (var phrase in RefusalPhrases)
        {
            if (window.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hasRefusalPhrase = true;
                break;
            }
        }
        if (!hasRefusalPhrase) return false;

        foreach (var word in NonLetterRegex().Split(window.ToString()))
        {
            if (word.Length > 0 && RefusalMetaVocabulary.Contains(word))
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
