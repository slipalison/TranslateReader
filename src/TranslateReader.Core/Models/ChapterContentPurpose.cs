namespace TranslateReader.Models;

/// <summary>
/// Why a chapter's HTML is being extracted. <see cref="Display"/> feeds the in-app WebView, so
/// image paths are rewritten to the <c>epub-images</c> virtual host and stylesheets are inlined.
/// <see cref="Export"/> feeds a file that leaves the app, where those app-only rewrites would be
/// dead URLs, so the chapter is returned exactly as the EPUB stores it.
/// </summary>
public enum ChapterContentPurpose
{
    Display,
    Export
}
