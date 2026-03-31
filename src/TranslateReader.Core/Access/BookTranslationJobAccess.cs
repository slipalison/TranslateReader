using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;
using TranslateReader.Models;

namespace TranslateReader.Access;

public class BookTranslationJobAccess(string connectionString) : IBookTranslationJobAccess
{
    private readonly string _connectionString = connectionString;

    public BookTranslationJobAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
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
            CREATE TABLE IF NOT EXISTS BookTranslationJobs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                SourceLanguage TEXT NOT NULL,
                TargetLanguage TEXT NOT NULL,
                Status TEXT NOT NULL,
                LastCompletedChapterIndex INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )
            """;
        command.ExecuteNonQuery();
    }

    public async Task<BookTranslationJob?> FetchActiveJobAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BookId, SourceLanguage, TargetLanguage, Status, LastCompletedChapterIndex, CreatedAt, UpdatedAt
            FROM BookTranslationJobs
            WHERE BookId = $bookId AND Status IN ('Pending', 'InProgress', 'Paused')
            ORDER BY UpdatedAt DESC
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$bookId", bookId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new BookTranslationJob
        {
            Id = reader.GetInt32(0),
            BookId = reader.GetInt32(1),
            SourceLanguage = reader.GetString(2),
            TargetLanguage = reader.GetString(3),
            Status = reader.GetString(4),
            LastCompletedChapterIndex = reader.GetInt32(5),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            UpdatedAt = DateTime.Parse(reader.GetString(7))
        };
    }

    public async Task SaveJobAsync(BookTranslationJob job)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BookTranslationJobs (BookId, SourceLanguage, TargetLanguage, Status, LastCompletedChapterIndex, CreatedAt, UpdatedAt)
            VALUES ($bookId, $source, $target, $status, $lastChapter, $created, $updated)
            """;
        command.Parameters.AddWithValue("$bookId", job.BookId);
        command.Parameters.AddWithValue("$source", job.SourceLanguage);
        command.Parameters.AddWithValue("$target", job.TargetLanguage);
        command.Parameters.AddWithValue("$status", job.Status);
        command.Parameters.AddWithValue("$lastChapter", job.LastCompletedChapterIndex);
        var now = DateTime.UtcNow.ToString("O");
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
        await command.ExecuteNonQueryAsync();

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid()";
        job.Id = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
    }

    public async Task UpdateJobProgressAsync(int jobId, int lastCompletedChapterIndex, string status)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE BookTranslationJobs
            SET LastCompletedChapterIndex = $lastChapter, Status = $status, UpdatedAt = $updated
            WHERE Id = $jobId
            """;
        command.Parameters.AddWithValue("$jobId", jobId);
        command.Parameters.AddWithValue("$lastChapter", lastCompletedChapterIndex);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteJobAsync(int jobId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BookTranslationJobs WHERE Id = $jobId";
        command.Parameters.AddWithValue("$jobId", jobId);
        await command.ExecuteNonQueryAsync();
    }
}
