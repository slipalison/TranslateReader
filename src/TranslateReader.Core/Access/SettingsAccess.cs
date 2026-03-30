using Microsoft.Data.Sqlite;
using TranslateReader.Contracts.Access;
using TranslateReader.Models;

namespace TranslateReader.Access;

public class SettingsAccess(string connectionString) : ISettingsAccess
{
    private readonly string _connectionString = connectionString;

    public SettingsAccess(string connectionString, bool initializeOnStartup) : this(connectionString)
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
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task<ReadingSettings> FetchSettingsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value FROM Settings";
        var values = new Dictionary<string, string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values[reader.GetString(0)] = reader.GetString(1);

        if (values.Count == 0)
            return new ReadingSettings();

        return new ReadingSettings
        {
            Theme = Enum.TryParse<ThemeType>(values.GetValueOrDefault("Theme"), out var theme) ? theme : ThemeType.Light,
            FontFamily = values.GetValueOrDefault("FontFamily") ?? "Georgia",
            FontSize = double.TryParse(values.GetValueOrDefault("FontSize"), out var fontSize) ? fontSize : 18,
            LineSpacing = double.TryParse(values.GetValueOrDefault("LineSpacing"), out var lineSpacing) ? lineSpacing : 1.6,
            LetterSpacing = double.TryParse(values.GetValueOrDefault("LetterSpacing"), out var letterSpacing) ? letterSpacing : 0,
            WordSpacing = double.TryParse(values.GetValueOrDefault("WordSpacing"), out var wordSpacing) ? wordSpacing : 0,
            ReadingMode = Enum.TryParse<ReadingMode>(values.GetValueOrDefault("ReadingMode"), out var readingMode) ? readingMode : ReadingMode.Scroll,
            TranslationModelName = values.GetValueOrDefault("TranslationModelName") ?? "gemma-2-2b",
            TranslationTemperature = double.TryParse(values.GetValueOrDefault("TranslationTemperature"), out var translationTemp) ? translationTemp : 0.1,
            SourceLanguage = values.GetValueOrDefault("SourceLanguage") ?? "English",
            TargetLanguage = values.GetValueOrDefault("TargetLanguage") ?? "Brazilian Portuguese (PT-BR)"
        };
    }

    public async Task SaveSettingsAsync(ReadingSettings settings)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        await UpsertValueAsync(connection, transaction, "Theme", settings.Theme.ToString());
        await UpsertValueAsync(connection, transaction, "FontFamily", settings.FontFamily);
        await UpsertValueAsync(connection, transaction, "FontSize", settings.FontSize.ToString());
        await UpsertValueAsync(connection, transaction, "LineSpacing", settings.LineSpacing.ToString());
        await UpsertValueAsync(connection, transaction, "LetterSpacing", settings.LetterSpacing.ToString());
        await UpsertValueAsync(connection, transaction, "WordSpacing", settings.WordSpacing.ToString());
        await UpsertValueAsync(connection, transaction, "ReadingMode", settings.ReadingMode.ToString());
        await UpsertValueAsync(connection, transaction, "TranslationModelName", settings.TranslationModelName);
        await UpsertValueAsync(connection, transaction, "TranslationTemperature", settings.TranslationTemperature.ToString());
        await UpsertValueAsync(connection, transaction, "SourceLanguage", settings.SourceLanguage);
        await UpsertValueAsync(connection, transaction, "TargetLanguage", settings.TargetLanguage);

        await transaction.CommitAsync();
    }

    private static async Task UpsertValueAsync(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ($key, $value)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }
}
