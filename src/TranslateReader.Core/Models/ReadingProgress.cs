namespace TranslateReader.Models;

public class ReadingProgress
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string ChapterHRef { get; set; } = string.Empty;
    public double ScrollPosition { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime UpdatedAt { get; set; }
}
