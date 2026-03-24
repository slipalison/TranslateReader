using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;
using TranslateReader.Models;

namespace TranslateReader.Access;

public class ReadingStateAccess(string connectionString) : IReadingStateAccess
{
    private readonly string _connectionString = connectionString;

    public ReadingStateAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
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
            CREATE TABLE IF NOT EXISTS ReadingProgress (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL UNIQUE,
                ChapterHRef TEXT NOT NULL,
                ScrollPosition REAL NOT NULL,
                ProgressPercentage REAL NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Bookmarks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                ChapterHRef TEXT NOT NULL,
                Position REAL NOT NULL,
                Label TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task<ReadingProgress?> FetchProgressAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ReadingProgress WHERE BookId = $bookId";
        command.Parameters.AddWithValue("$bookId", bookId);
        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;
        return MapProgress(reader);
    }

    public async Task SaveProgressAsync(ReadingProgress progress)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ReadingProgress (BookId, ChapterHRef, ScrollPosition, ProgressPercentage, UpdatedAt)
            VALUES ($bookId, $href, $scroll, $pct, $updated)
            ON CONFLICT(BookId) DO UPDATE SET
                ChapterHRef = excluded.ChapterHRef,
                ScrollPosition = excluded.ScrollPosition,
                ProgressPercentage = excluded.ProgressPercentage,
                UpdatedAt = excluded.UpdatedAt
            """;
        command.Parameters.AddWithValue("$bookId", progress.BookId);
        command.Parameters.AddWithValue("$href", progress.ChapterHRef);
        command.Parameters.AddWithValue("$scroll", progress.ScrollPosition);
        command.Parameters.AddWithValue("$pct", progress.ProgressPercentage);
        command.Parameters.AddWithValue("$updated", progress.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Bookmark>> FetchBookmarksAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Bookmarks WHERE BookId = $bookId ORDER BY CreatedAt";
        command.Parameters.AddWithValue("$bookId", bookId);
        var bookmarks = new List<Bookmark>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bookmarks.Add(MapBookmark(reader));
        return bookmarks;
    }

    public async Task SaveBookmarkAsync(Bookmark bookmark)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Bookmarks (BookId, ChapterHRef, Position, Label, CreatedAt)
            VALUES ($bookId, $href, $pos, $label, $created)
            """;
        command.Parameters.AddWithValue("$bookId", bookmark.BookId);
        command.Parameters.AddWithValue("$href", bookmark.ChapterHRef);
        command.Parameters.AddWithValue("$pos", bookmark.Position);
        command.Parameters.AddWithValue("$label", bookmark.Label);
        command.Parameters.AddWithValue("$created", bookmark.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveBookmarkAsync(int bookmarkId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Bookmarks WHERE Id = $id";
        command.Parameters.AddWithValue("$id", bookmarkId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveStateForBookAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var deleteProgress = connection.CreateCommand();
        deleteProgress.CommandText = "DELETE FROM ReadingProgress WHERE BookId = $bookId";
        deleteProgress.Parameters.AddWithValue("$bookId", bookId);
        await deleteProgress.ExecuteNonQueryAsync();

        using var deleteBookmarks = connection.CreateCommand();
        deleteBookmarks.CommandText = "DELETE FROM Bookmarks WHERE BookId = $bookId";
        deleteBookmarks.Parameters.AddWithValue("$bookId", bookId);
        await deleteBookmarks.ExecuteNonQueryAsync();
    }

    private static ReadingProgress MapProgress(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        BookId = reader.GetInt32(1),
        ChapterHRef = reader.GetString(2),
        ScrollPosition = reader.GetDouble(3),
        ProgressPercentage = reader.GetDouble(4),
        UpdatedAt = DateTime.Parse(reader.GetString(5))
    };

    private static Bookmark MapBookmark(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        BookId = reader.GetInt32(1),
        ChapterHRef = reader.GetString(2),
        Position = reader.GetDouble(3),
        Label = reader.GetString(4),
        CreatedAt = DateTime.Parse(reader.GetString(5))
    };
}
