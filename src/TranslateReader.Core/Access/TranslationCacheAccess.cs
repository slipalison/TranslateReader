using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;

namespace TranslateReader.Access;

public class TranslationCacheAccess(string connectionString) : ITranslationCacheAccess
{
    private readonly string _connectionString = connectionString;

    public TranslationCacheAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
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
            CREATE TABLE IF NOT EXISTS TranslationCache (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                ChapterHRef TEXT NOT NULL,
                OriginalHash TEXT NOT NULL,
                TranslatedText TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UNIQUE(BookId, ChapterHRef, OriginalHash)
            )
            """;
        command.ExecuteNonQuery();
    }

    public async Task<string?> FetchTranslationAsync(int bookId, string chapterHRef, string originalHash)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TranslatedText FROM TranslationCache
            WHERE BookId = $bookId AND ChapterHRef = $href AND OriginalHash = $hash
            """;
        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$href", chapterHRef);
        command.Parameters.AddWithValue("$hash", originalHash);
        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SaveTranslationAsync(int bookId, string chapterHRef, string originalHash, string translatedText)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO TranslationCache (BookId, ChapterHRef, OriginalHash, TranslatedText, CreatedAt)
            VALUES ($bookId, $href, $hash, $text, $created)
            ON CONFLICT(BookId, ChapterHRef, OriginalHash) DO UPDATE SET
                TranslatedText = excluded.TranslatedText,
                CreatedAt = excluded.CreatedAt
            """;
        command.Parameters.AddWithValue("$bookId", bookId);
        command.Parameters.AddWithValue("$href", chapterHRef);
        command.Parameters.AddWithValue("$hash", originalHash);
        command.Parameters.AddWithValue("$text", translatedText);
        command.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveTranslationsForBookAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM TranslationCache WHERE BookId = $bookId";
        command.Parameters.AddWithValue("$bookId", bookId);
        await command.ExecuteNonQueryAsync();
    }
}
