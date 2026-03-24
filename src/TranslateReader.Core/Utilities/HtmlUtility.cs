using System.Text.RegularExpressions;

namespace TranslateReader.Utilities;

public static class HtmlUtility
{
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
}
