using TranslateReader.Contracts.Utilities;

namespace TranslateReader.Utilities;

public class PromptUtility : IPromptUtility
{
    public (string SystemMessage, string UserMessage) BuildTranslationMessages(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle,
        string? previousParagraph)
    {
        var systemMessage = BuildSystemMessage(sourceLanguage, targetLanguage, bookTitle, chapterTitle, previousParagraph);
        return (systemMessage, text);
    }

    public (string SystemMessage, string UserMessage) BuildSnippetTranslationMessages(
        string snippet,
        string paragraph,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle)
    {
        var systemMessage = BuildSnippetSystemMessage(
            snippet, paragraph, sourceLanguage, targetLanguage, bookTitle, chapterTitle);
        return (systemMessage, snippet);
    }

    private static string BuildSystemMessage(
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle,
        string? previousParagraph)
    {
        var parts = new List<string>
        {
            $"Translate the following {sourceLanguage} text to {targetLanguage}.",
            "Provide only the translation, no explanations.",
            "Keep proper nouns unchanged.",
            "Translate naturally, not literally."
        };

        if (!string.IsNullOrWhiteSpace(bookTitle))
            parts.Add($"Book: {bookTitle}");

        if (!string.IsNullOrWhiteSpace(chapterTitle))
            parts.Add($"Chapter: {chapterTitle}");

        if (!string.IsNullOrWhiteSpace(previousParagraph))
            parts.Add($"Previous paragraph for context: {previousParagraph}");

        return string.Join(" ", parts);
    }

    // Hardened for small local models (D-2026-08-09-snippet-translation-5): the excerpt and the
    // paragraph are BOTH delimited by triple quotes, and the excerpt is restated here in addition
    // to arriving as the user message, because a weak model was observed translating the whole
    // paragraph — including other snippets' chip labels baked into a polluted context — instead of
    // only the newly selected excerpt.
    private static string BuildSnippetSystemMessage(
        string snippet,
        string paragraph,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle)
    {
        var parts = new List<string>
        {
            $"Translate the following {sourceLanguage} sentence excerpt to {targetLanguage}.",
            $"The excerpt to translate is delimited below by triple quotes, and it is the ONLY text you must translate: \"\"\"{snippet}\"\"\"",
            "Respond with EXCLUSIVELY the direct translation of that delimited excerpt: no delimiters, no labels, no comments, nothing added.",
            "Keep proper nouns unchanged.",
            "Translate naturally, not literally."
        };

        if (!string.IsNullOrWhiteSpace(bookTitle))
            parts.Add($"Book: {bookTitle}");

        if (!string.IsNullOrWhiteSpace(chapterTitle))
            parts.Add($"Chapter: {chapterTitle}");

        parts.Add(
            "A paragraph is given below, delimited by triple quotes, for disambiguation context only: " +
            "do not translate it, do not repeat any part of it, and do not include any of it in your answer.");
        parts.Add($"Paragraph for context: \"\"\"{paragraph}\"\"\"");

        return string.Join(" ", parts);
    }
}
