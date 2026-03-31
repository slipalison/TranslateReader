namespace TranslateReader.Models;

public class BookTranslationJob
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int LastCompletedChapterIndex { get; set; } = -1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
