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
}
