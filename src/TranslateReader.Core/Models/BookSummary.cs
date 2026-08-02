namespace TranslateReader.Models;

public class BookSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public DateTime? LastReadAt { get; set; }
    public int TotalChapters { get; set; }
}
