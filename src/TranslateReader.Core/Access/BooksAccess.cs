using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;
using TranslateReader.Models;

namespace TranslateReader.Access;

public class BooksAccess(string connectionString) : IBooksAccess
{
    private readonly string _connectionString = connectionString;

    public BooksAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
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
            CREATE TABLE IF NOT EXISTS Books (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                Publisher TEXT NOT NULL,
                Language TEXT NOT NULL,
                CoverImagePath TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                TotalChapters INTEGER NOT NULL,
                DateAdded TEXT NOT NULL,
                LastOpenedAt TEXT
            );
            CREATE TABLE IF NOT EXISTS Chapters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BookId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                OrderIndex INTEGER NOT NULL,
                HRef TEXT NOT NULL,
                FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE CASCADE
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<Book>> FetchAllBooksAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Books ORDER BY LastOpenedAt DESC, DateAdded DESC";
        var books = new List<Book>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            books.Add(MapBook(reader));
        return books;
    }

    public async Task<Book> FetchBookAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Books WHERE Id = $id";
        command.Parameters.AddWithValue("$id", bookId);
        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException($"Book {bookId} not found.");
        return MapBook(reader);
    }

    public async Task<int> SaveBookAsync(Book book)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Books (Title, Author, Publisher, Language, CoverImagePath, FilePath, TotalChapters, DateAdded, LastOpenedAt)
            VALUES ($title, $author, $publisher, $language, $cover, $file, $total, $added, $opened);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$title", book.Title);
        command.Parameters.AddWithValue("$author", book.Author);
        command.Parameters.AddWithValue("$publisher", book.Publisher);
        command.Parameters.AddWithValue("$language", book.Language);
        command.Parameters.AddWithValue("$cover", book.CoverImagePath);
        command.Parameters.AddWithValue("$file", book.FilePath);
        command.Parameters.AddWithValue("$total", book.TotalChapters);
        command.Parameters.AddWithValue("$added", book.DateAdded.ToString("O"));
        command.Parameters.AddWithValue("$opened", book.LastOpenedAt?.ToString("O") ?? (object)DBNull.Value);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task SaveChaptersAsync(IEnumerable<Chapter> chapters)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        foreach (var chapter in chapters)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Chapters (BookId, Title, OrderIndex, HRef)
                VALUES ($bookId, $title, $order, $href)
                """;
            command.Parameters.AddWithValue("$bookId", chapter.BookId);
            command.Parameters.AddWithValue("$title", chapter.Title);
            command.Parameters.AddWithValue("$order", chapter.OrderIndex);
            command.Parameters.AddWithValue("$href", chapter.HRef);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task RemoveBookAsync(int bookId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Books WHERE Id = $id";
        command.Parameters.AddWithValue("$id", bookId);
        await command.ExecuteNonQueryAsync();
    }

    private static Book MapBook(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        Author = reader.GetString(2),
        Publisher = reader.GetString(3),
        Language = reader.GetString(4),
        CoverImagePath = reader.GetString(5),
        FilePath = reader.GetString(6),
        TotalChapters = reader.GetInt32(7),
        DateAdded = DateTime.Parse(reader.GetString(8)),
        LastOpenedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
    };
}
