using Microsoft.Data.Sqlite;

namespace TranslateReader.Tests;

/// <summary>
/// Mantém uma conexão SQLite in-memory aberta durante o teste,
/// evitando que o banco seja destruído entre operações.
/// </summary>
public sealed class InMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _anchor;

    public string ConnectionString { get; }

    public InMemoryDatabase()
    {
        var name = Guid.NewGuid().ToString("N");
        ConnectionString = $"Data Source={name};Mode=Memory;Cache=Shared";
        _anchor = new SqliteConnection(ConnectionString);
        _anchor.Open();
    }

    public void Dispose() => _anchor.Dispose();
}
