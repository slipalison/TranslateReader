using System.Text;
using System.Text.RegularExpressions;

namespace TranslateReader.Utilities;

public static partial class HtmlUtility
{
    // Book HTML is untrusted input (csharp.md 4): every regex here is bounded so a
    // pathological chapter cannot pin a thread (S6444 / ReDoS).
    private const int RegexTimeoutMilliseconds = 1000;

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
        var matches = TextBlockRegex().Matches(bodyContent);
        return matches
            .Select(m => StripHtmlTags(m.Groups[2].Value).Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    public static string ReplaceTextBlocksInHtml(string html, IReadOnlyList<string> translations)
    {
        var index = 0;
        return TextBlockRegex().Replace(html, match =>
        {
            var innerHtml = match.Groups[2].Value;
            var text = StripHtmlTags(innerHtml).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return match.Value;
            if (index >= translations.Count)
                return match.Value;
            var translated = System.Net.WebUtility.HtmlEncode(translations[index++]);
            var tag = match.Groups[1].Value;
            var openTag = match.Value[..(match.Value.IndexOf('>') + 1)];
            return $"{openTag}{translated}</{tag}>";
        });
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

    // CSS goes right before </head> so it wins the cascade over the EPUB's own stylesheets;
    // headOpenEnd is the fallback anchor and stays valid after the base tag insertion,
    // which happens at that same offset.
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

    // SYSLIB1044: the backreference \1 blocks the source generator from emitting a complete
    // implementation, so it falls back to the interpreted engine. Waiver per
    // D-2026-07-30-sonar-zero-issues-3 mechanism (c): rewriting the pattern to drop the
    // backreference would change which closing tag is matched, i.e. change behavior.
#pragma warning disable SYSLIB1044
    [GeneratedRegex(@"<(p|h[1-6]|li)\b[^>]*>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexTimeoutMilliseconds)]
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
