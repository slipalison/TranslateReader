namespace TranslateReader.Models;

public class Bookmark
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string ChapterHRef { get; set; } = string.Empty;
    public double Position { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
