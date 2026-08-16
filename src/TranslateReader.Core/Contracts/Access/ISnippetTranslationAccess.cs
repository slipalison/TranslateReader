using TranslateReader.Models;

namespace TranslateReader.Contracts.Access;

/// <summary>
/// CRUD for per-sentence-range snippet translations, keyed by book, chapter and sentence anchor.
/// </summary>
public interface ISnippetTranslationAccess
{
    /// <summary>Returns every snippet saved for the chapter, ordered by paragraph then sentence start.</summary>
    Task<IReadOnlyList<SnippetTranslation>> FetchSnippetsAsync(int bookId, string chapterHRef);

    /// <summary>
    /// Saves a snippet, destructively replacing any existing snippet in the same paragraph whose
    /// sentence range overlaps the one being saved.
    /// </summary>
    Task SaveSnippetAsync(SnippetTranslation snippet);

    Task RemoveSnippetAsync(
        int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd);

    Task SetShowingOriginalAsync(
        int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd,
        bool showingOriginal);

    Task RemoveSnippetsForBookAsync(int bookId);
}
