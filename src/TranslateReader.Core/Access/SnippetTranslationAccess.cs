using System.Globalization;
using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;
using TranslateReader.Models;

namespace TranslateReader.Access;

public class SnippetTranslationAccess(string connectionString) : ISnippetTranslationAccess
{
    private readonly string _connectionString = connectionString;

    public SnippetTranslationAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
    {
        if (initializeOnStartup)
            InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SnippetTranslations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                ChapterHRef TEXT NOT NULL,
                ParagraphIndex INTEGER NOT NULL,
                SentenceStart INTEGER NOT NULL,
                SentenceEnd INTEGER NOT NULL,
                OriginalHash TEXT NOT NULL,
                TranslatedText TEXT NOT NULL,
                ShowingOriginal INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UNIQUE(BookId, ChapterHRef, ParagraphIndex, SentenceStart, SentenceEnd)
            )
            """;
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<SnippetTranslation>> FetchSnippetsAsync(int bookId, string chapterHRef)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BookId, ChapterHRef, ParagraphIndex, SentenceStart, SentenceEnd, OriginalHash,
                   TranslatedText, ShowingOriginal, CreatedAt
            FROM SnippetTranslations
            WHERE BookId = $bookId AND ChapterHRef = $href
            ORDER BY ParagraphIndex, SentenceStart
            """;
        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$href", chapterHRef);

        var results = new List<SnippetTranslation>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(ReadSnippet(reader));
        return results;
    }

    // The destructive-overlap rule (!(o.b < a || o.a > b)) lives in the DELETE's WHERE clause, and
    // both statements share one transaction: a crash between them can never leave two overlapping
    // ranges translated in the same paragraph.
    public async Task SaveSnippetAsync(SnippetTranslation snippet)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = """
                DELETE FROM SnippetTranslations
                WHERE BookId = $bookId AND ChapterHRef = $href AND ParagraphIndex = $paragraph
                  AND NOT (SentenceEnd < $start OR SentenceStart > $end)
                """;
            AddAnchorParameters(delete, snippet.BookId, snippet.ChapterHRef, snippet.ParagraphIndex,
                snippet.SentenceStart, snippet.SentenceEnd);
            await delete.ExecuteNonQueryAsync();
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO SnippetTranslations
                    (BookId, ChapterHRef, ParagraphIndex, SentenceStart, SentenceEnd, OriginalHash,
                     TranslatedText, ShowingOriginal, CreatedAt)
                VALUES
                    ($bookId, $href, $paragraph, $start, $end, $hash, $text, $showing, $created)
                """;
            AddAnchorParameters(insert, snippet.BookId, snippet.ChapterHRef, snippet.ParagraphIndex,
                snippet.SentenceStart, snippet.SentenceEnd);
            insert.Parameters.AddWithValue("$hash", snippet.OriginalHash);
            insert.Parameters.AddWithValue("$text", snippet.TranslatedText);
            insert.Parameters.AddWithValue("$showing", snippet.ShowingOriginal ? 1 : 0);
            insert.Parameters.AddWithValue("$created", snippet.CreatedAt.ToString("O"));
            await insert.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task RemoveSnippetAsync(
        int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM SnippetTranslations
            WHERE BookId = $bookId AND ChapterHRef = $href AND ParagraphIndex = $paragraph
              AND SentenceStart = $start AND SentenceEnd = $end
            """;
        AddAnchorParameters(command, bookId, chapterHRef, paragraphIndex, sentenceStart, sentenceEnd);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetShowingOriginalAsync(
        int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd,
        bool showingOriginal)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE SnippetTranslations SET ShowingOriginal = $showing
            WHERE BookId = $bookId AND ChapterHRef = $href AND ParagraphIndex = $paragraph
              AND SentenceStart = $start AND SentenceEnd = $end
            """;
        AddAnchorParameters(command, bookId, chapterHRef, paragraphIndex, sentenceStart, sentenceEnd);
        command.Parameters.AddWithValue("$showing", showingOriginal ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveSnippetsForBookAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SnippetTranslations WHERE BookId = $bookId";
        command.Parameters.AddWithValue("$bookId", bookId);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddAnchorParameters(
        SqliteCommand command, int bookId, string chapterHRef, int paragraphIndex,
        int sentenceStart, int sentenceEnd)
    {
        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$href", chapterHRef);
        command.Parameters.AddWithValue("$paragraph", paragraphIndex);
        command.Parameters.AddWithValue("$start", sentenceStart);
        command.Parameters.AddWithValue("$end", sentenceEnd);
    }

    private static SnippetTranslation ReadSnippet(SqliteDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetInt32(1),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.GetInt32(4),
        reader.GetInt32(5),
        reader.GetString(6),
        reader.GetString(7),
        reader.GetInt32(8) != 0,
        DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture));
}
