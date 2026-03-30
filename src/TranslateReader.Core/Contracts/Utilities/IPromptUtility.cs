namespace TranslateReader.Contracts.Utilities;

public interface IPromptUtility
{
    (string SystemMessage, string UserMessage) BuildTranslationMessages(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? bookTitle,
        string? chapterTitle,
        string? previousParagraph);
}
