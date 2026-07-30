using System.Text;
using System.Text.RegularExpressions;

namespace TranslateReader.Utilities;

public static partial class HtmlUtility
{
    public static string ExtractBodyContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var bodyStart = Regex.Match(html, @"<body\b[^>]*>", RegexOptions.IgnoreCase);
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
        if (string.IsNullOrWhiteSpace(html))
        {
            var headContent = (baseTag ?? "") + (css ?? "");
            return $"<html><head>{headContent}</head><body></body></html>";
        }

        bool hasBase = !string.IsNullOrEmpty(baseTag) && !html.Contains("<base ", StringComparison.OrdinalIgnoreCase);
        bool hasCss = !string.IsNullOrEmpty(css);

        if (!hasBase && !hasCss) return html;

        var result = html;

        // Base tag: inject right after <head> (before EPUB content, needed for URL resolution)
        if (hasBase)
        {
            var headMatch = Regex.Match(result, @"<head\b[^>]*>", RegexOptions.IgnoreCase);
            if (headMatch.Success)
            {
                result = result.Insert(headMatch.Index + headMatch.Length, "\n" + baseTag);
            }
            else
            {
                return BuildFallbackHtml(result, baseTag, css);
            }
        }

        // CSS: inject right before </head> (after all EPUB CSS, for cascade priority)
        if (hasCss)
        {
            var endHeadIndex = result.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (endHeadIndex >= 0)
            {
                result = result.Insert(endHeadIndex, "\n" + css + "\n");
            }
            else
            {
                var headMatch = Regex.Match(result, @"<head\b[^>]*>", RegexOptions.IgnoreCase);
                if (headMatch.Success)
                    result = result.Insert(headMatch.Index + headMatch.Length, "\n" + css);
                else
                    return BuildFallbackHtml(result, baseTag, css);
            }
        }

        return result;
    }

    private static string BuildFallbackHtml(string html, string? baseTag, string? css)
    {
        var headContent = (baseTag ?? "") + (css ?? "");

        var htmlMatch = Regex.Match(html, @"<html\b[^>]*>", RegexOptions.IgnoreCase);
        if (htmlMatch.Success)
            return html.Insert(htmlMatch.Index + htmlMatch.Length, $"\n<head>{headContent}\n</head>");

        var bodyMatch = Regex.Match(html, @"<body\b[^>]*>", RegexOptions.IgnoreCase);
        if (bodyMatch.Success)
        {
            var result = html.Insert(bodyMatch.Index, $"\n<head>{headContent}\n</head>\n");
            if (!Regex.IsMatch(result, @"<html\b", RegexOptions.IgnoreCase))
                result = "<html>" + result + "</html>";
            return result;
        }

        var xmlMatch = Regex.Match(html, @"<\?xml\b[^>]*\?>", RegexOptions.IgnoreCase);
        if (xmlMatch.Success)
            return html.Insert(xmlMatch.Index + xmlMatch.Length,
                $"\n<html>\n<head>{headContent}\n</head>\n<body>") + "\n</body>\n</html>";

        return $"<html><head>{headContent}</head><body>{html}</body></html>";
    }

    [GeneratedRegex(@"<p\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"<(p|h[1-6]|li)\b[^>]*>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TextBlockRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
