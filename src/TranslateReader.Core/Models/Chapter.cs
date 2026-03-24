namespace TranslateReader.Models;

public class Chapter
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string HRef { get; set; } = string.Empty;
}
