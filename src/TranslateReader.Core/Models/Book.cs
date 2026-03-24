namespace TranslateReader.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string CoverImagePath { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int TotalChapters { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime? LastOpenedAt { get; set; }
}
