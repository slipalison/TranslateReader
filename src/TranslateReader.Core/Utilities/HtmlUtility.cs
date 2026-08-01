using System.Text;
using System.Text.RegularExpressions;

namespace TranslateReader.Utilities;

public static partial class HtmlUtility
{
    // Book HTML is untrusted input (csharp.md 4): every regex here is bounded so a
    // pathological chapter cannot pin a thread (S6444 / ReDoS).
    private const int RegexTimeoutMilliseconds = 1000;

    private const string TagGroup = "tag";
    private const string TextGroup = "text";
    private const string DivTag = "div";

    public static string ExtractBodyContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var bodyStart = BodyOpenTagRegex().Match(html);
        if (!bodyStart.Success) return html;
        var bodyEndIndex = html.IndexOf("</body>", bodyStart.Index + bodyStart.Length, StringComparison.OrdinalIgnoreCase);
        if (bodyEndIndex < 0) return html[(bodyStart.Index + bodyStart.Length)..];
        return html[(bodyStart.Index + bodyStart.Length)..bodyEndIndex];
    }

    public static List<string> ExtractParagraphs(string bodyContent)
    {
        var matches = ParagraphRegex().Matches(bodyContent);
        return matches
            .Select(m => StripHtmlTags(m.Groups[1].Value).Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    public static List<string> ExtractTextBlocks(string bodyContent)
    {
        var blocks = new List<string>();
        foreach (Match match in TextBlockRegex().Matches(bodyContent))
        {
            var text = BlockText(match);
            if (IsTranslatableBlock(match, text))
                blocks.Add(text);
        }
        return blocks;
    }

    public static string ReplaceTextBlocksInHtml(string html, IReadOnlyList<string> translations)
    {
        var index = 0;
        return TextBlockRegex().Replace(html, match =>
        {
            var text = BlockText(match);
            if (!IsTranslatableBlock(match, text))
                return match.Value;
            if (index >= translations.Count)
                return match.Value;
            var translated = System.Net.WebUtility.HtmlEncode(translations[index++]);
            var tag = match.Groups[TagGroup].Value;
            var openTag = match.Value[..(match.Value.IndexOf('>') + 1)];
            return $"{openTag}{translated}</{tag}>";
        });
    }

    private static string BlockText(Match match) =>
        StripHtmlTags(match.Groups[TextGroup].Value).Trim();

    // Extraction and replacement share this predicate (decision
    // D-2026-08-01-div-paragraph-translation-8). Both passes walk the same matches in document
    // order, so block number N always pairs with translation number N. The div branch needs the
    // stricter guard because calibre wraps images, bullets and stray numbers in the very same leaf
    // div it uses for prose. The paragraph branch keeps the whitespace filter it always had, since
    // hardening it would change what the three real EPUB fixtures extract.
    private static bool IsTranslatableBlock(Match match, string text) =>
        match.Groups[TagGroup].ValueSpan.Equals(DivTag, StringComparison.OrdinalIgnoreCase)
            ? ContainsLetter(text)
            : !string.IsNullOrWhiteSpace(text);

    private static bool ContainsLetter(string text) => text.Any(char.IsLetter);

    public static int CountTextChars(string html)
    {
        return StripHtmlTags(html).Count(c => !char.IsWhiteSpace(c));
    }

    public static string StripHtmlTags(string html) =>
        HtmlTagRegex().Replace(html, string.Empty);

    public static string BuildContinuousScrollHtml(
        IReadOnlyList<(string href, string bodyContent)> chapters)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < chapters.Count; i++)
        {
            if (i > 0)
                sb.Append("<hr class=\"chapter-separator\" />");
            sb.Append($"<div class=\"chapter-content\" data-chapter-href=\"{chapters[i].href}\" data-chapter-index=\"{i}\">");
            sb.Append(chapters[i].bodyContent);
            sb.Append("</div>");
        }
        return sb.ToString();
    }

    public static string InjectTags(string html, string? baseTag, string? css)
    {
        if (string.IsNullOrWhiteSpace(html)) return BuildEmptyDocument(baseTag, css);

        var pendingBase = ResolvePendingBaseTag(html, baseTag);
        var pendingCss = css ?? string.Empty;
        if (pendingBase.Length == 0 && pendingCss.Length == 0) return html;

        var headOpen = HeadOpenTagRegex().Match(html);
        if (!CanInjectIntoHead(html, headOpen.Success, pendingBase.Length > 0))
            return BuildFallbackHtml(html, baseTag, css);

        var headOpenEnd = headOpen.Index + headOpen.Length;
        var withBase = pendingBase.Length == 0 ? html : html.Insert(headOpenEnd, "\n" + pendingBase);
        return pendingCss.Length == 0 ? withBase : InjectCss(withBase, pendingCss, headOpenEnd);
    }

    private static string BuildEmptyDocument(string? baseTag, string? css) =>
        $"<html><head>{baseTag ?? ""}{css ?? ""}</head><body></body></html>";

    private static string ResolvePendingBaseTag(string html, string? baseTag) =>
        string.IsNullOrEmpty(baseTag) || html.Contains("<base ", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : baseTag;

    private static bool CanInjectIntoHead(string html, bool headOpenFound, bool injectingBase) =>
        headOpenFound || (!injectingBase && html.Contains("</head>", StringComparison.OrdinalIgnoreCase));

    // The reader stylesheet must come last inside the head element, otherwise the stylesheets
    // shipped with the EPUB win the cascade. The head opening offset is the fallback anchor and
    // stays valid after a base tag is inserted, because that insertion happens at the same offset.
    private static string InjectCss(string html, string css, int headOpenEnd)
    {
        var endHeadIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return endHeadIndex >= 0
            ? html.Insert(endHeadIndex, "\n" + css + "\n")
            : html.Insert(headOpenEnd, "\n" + css);
    }

    private static string BuildFallbackHtml(string html, string? baseTag, string? css)
    {
        var headContent = (baseTag ?? "") + (css ?? "");

        var htmlMatch = HtmlOpenTagRegex().Match(html);
        if (htmlMatch.Success)
            return html.Insert(htmlMatch.Index + htmlMatch.Length, $"\n<head>{headContent}\n</head>");

        var bodyMatch = BodyOpenTagRegex().Match(html);
        if (bodyMatch.Success)
        {
            var result = html.Insert(bodyMatch.Index, $"\n<head>{headContent}\n</head>\n");
            if (!HtmlTagPresenceRegex().IsMatch(result))
                result = "<html>" + result + "</html>";
            return result;
        }

        var xmlMatch = XmlDeclarationRegex().Match(html);
        if (xmlMatch.Success)
            return html.Insert(xmlMatch.Index + xmlMatch.Length,
                $"\n<html>\n<head>{headContent}\n</head>\n<body>") + "\n</body>\n</html>";

        return $"<html><head>{headContent}</head><body>{html}</body></html>";
    }

    [GeneratedRegex(@"<p\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMilliseconds)]
    private static partial Regex ParagraphRegex();

    // Disjoint union in a single alternation (D-2026-08-01-div-paragraph-translation-7): the p/h/li
    // branch is tried first, and the div branch only matches a LEAF div - one whose content holds no
    // <div, <p, <h1-6 or <li. The two sources therefore never overlap, which is what keeps the
    // extraction list paired 1:1 with the replacement walk instead of double counting the ~11k words
    // that calibre exports keep inside leaf divs.
    // SYSLIB1044: the backreference \k<tag> blocks the source generator from emitting a complete
    // implementation, so it falls back to the interpreted engine. Waiver per
    // D-2026-07-30-sonar-zero-issues-3 mechanism (c): rewriting the pattern to drop the
    // backreference would change which closing tag is matched, i.e. change behavior.
#pragma warning disable SYSLIB1044
    [GeneratedRegex(
        @"<(?<tag>p|h[1-6]|li)\b[^>]*>(?<text>.*?)</\k<tag>>" +
        @"|<(?<tag>div)\b[^>]*>(?<text>(?:(?!<div\b|</div|<p\b|<h[1-6]\b|<li\b).)*)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMilliseconds)]
    private static partial Regex TextBlockRegex();
#pragma warning restore SYSLIB1044

    [GeneratedRegex(@"<[^>]+>", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<body\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeoutMilliseconds)]
    private static partial Regex BodyOpenTagRegex();

    [GeneratedRegex(@"<head\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeoutMilliseconds)]
    private static partial Regex HeadOpenTagRegex();

    [GeneratedRegex(@"<html\b[^>]*>", RegexOptions.IgnoreCase, RegexTimeoutMilliseconds)]
    private static partial Regex HtmlOpenTagRegex();

    [GeneratedRegex(@"<html\b", RegexOptions.IgnoreCase, RegexTimeoutMilliseconds)]
    private static partial Regex HtmlTagPresenceRegex();

    [GeneratedRegex(@"<\?xml\b[^>]*\?>", RegexOptions.IgnoreCase, RegexTimeoutMilliseconds)]
    private static partial Regex XmlDeclarationRegex();
}
