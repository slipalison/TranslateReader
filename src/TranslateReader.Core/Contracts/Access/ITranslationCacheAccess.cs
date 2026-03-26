namespace TranslateReader.Contracts.Access;

public interface ITranslationCacheAccess
{
    Task<string?> FetchTranslationAsync(int bookId, string chapterHRef, string originalHash);
    Task SaveTranslationAsync(int bookId, string chapterHRef, string originalHash, string translatedText);
    Task RemoveTranslationsForBookAsync(int bookId);
}
