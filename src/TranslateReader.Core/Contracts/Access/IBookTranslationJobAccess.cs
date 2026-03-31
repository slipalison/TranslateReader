using TranslateReader.Models;

namespace TranslateReader.Contracts.Access;

public interface IBookTranslationJobAccess
{
    Task<BookTranslationJob?> FetchActiveJobAsync(int bookId);
    Task SaveJobAsync(BookTranslationJob job);
    Task UpdateJobProgressAsync(int jobId, int lastCompletedChapterIndex, string status);
    Task DeleteJobAsync(int jobId);
}
