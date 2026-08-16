using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

/// <summary>
/// Orchestrates translation of individual sentence-range snippets, independent of the
/// chapter/paragraph translation flow on <see cref="ITranslationManager"/>.
/// </summary>
public interface ISnippetTranslationManager
{
    /// <summary>
    /// Translates the requested sentence range, using the surrounding paragraph only as context,
    /// caches the result, and persists it as the book's active snippet for that range.
    /// </summary>
    Task<SnippetTranslation> TranslateSnippetAsync(
        int bookId, SnippetRequest request, string sourceLanguage, string targetLanguage, CancellationToken ct);

    Task<IReadOnlyList<SnippetTranslation>> FetchSnippetsAsync(int bookId, string chapterHRef);

    Task SetShowingOriginalAsync(int bookId, SnippetToggleRequest request);

    Task RemoveSnippetAsync(int bookId, SnippetRemoveRequest request);
}
