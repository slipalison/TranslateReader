namespace TranslateReader.Tests;

/// <summary>
/// The two calibre bodies of the phase CONTEXT (`div-paragraph-translation`, section Notes).
/// They live here because two suites depend on the exact same characters: `HtmlUtilityTests`
/// asserts which blocks come out of them, and `TranslationManagerTests` asserts the coverage
/// ratio derived from them (106/113). Kept as one copy so editing the markup cannot make one
/// suite exercise a different body than the other.
/// </summary>
internal static class CalibreFixtures
{
    /// <summary>
    /// Fixture A: calibre exports wrap every paragraph in a `div class="calibreN"` and never emit
    /// a single `p`. The container div, the image-only div and the bullet-only div must stay out
    /// of the block list; the bullet is the 7 non-space characters that no block covers, which is
    /// what pushes the coverage ratio below 1.0.
    /// </summary>
    public const string PartiallyCoveredBody = """
        <div class="calibre1">
        <div class="calibre2">First calibre paragraph with real text.</div>
        <div class="calibre2">Second calibre paragraph with more text.</div>
        <div class="calibre3"><img src="fig1.png"/></div>
        <div class="calibre2">&#8226;</div>
        <div class="calibre2">Third paragraph, letters only matter here.</div>
        </div>
        """;

    /// <summary>
    /// Fixture B: every non-space character lives inside the single leaf div.
    /// </summary>
    public const string FullyCoveredBody =
        "<div class=\"calibre2\">Only paragraph, fully covered by the leaf div.</div>";
}
