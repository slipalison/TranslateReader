using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using VersOne.Epub;
using VersOne.Epub.Options;
using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

using TranslateReader.Utilities;

namespace TranslateReader.Business.Engines;

public partial class ParsingEngine : IParsingEngine
{
    public async Task<Book> ExtractMetadataAsync(string filePath)
    {
        var epub = await ReadEpubSafeAsync(filePath);
        var metadata = epub.Schema.Package.Metadata;
        return new Book
        {
            Title = epub.Title ?? string.Empty,
            Author = string.Join(", ", epub.AuthorList ?? []),
            Publisher = metadata.Publishers.FirstOrDefault()?.Publisher ?? string.Empty,
            Language = metadata.Languages.FirstOrDefault()?.Language ?? string.Empty,
            CoverImagePath = string.Empty,
            FilePath = filePath,
            TotalChapters = epub.ReadingOrder.Count,
            DateAdded = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<Chapter>> ExtractChaptersAsync(string filePath)
    {
        var epub = await ReadEpubSafeAsync(filePath);
        return epub.ReadingOrder
            .Select((item, index) => new Chapter
            {
                Title = ResolveChapterTitle(epub, item.FilePath),
                OrderIndex = index,
                HRef = item.FilePath
            })
            .ToList();
    }

    public async Task<string> ExtractChapterContentAsync(string filePath, string chapterHRef, string imagesDirectory)
    {
        var epub = await ReadEpubSafeAsync(filePath);
        var item = epub.ReadingOrder.FirstOrDefault(r => r.FilePath == chapterHRef)
            ?? throw new InvalidOperationException($"Chapter '{chapterHRef}' not found in EPUB.");
        if (string.IsNullOrEmpty(item.Content))
            throw new InvalidOperationException($"Chapter '{chapterHRef}' has no content.");
        var html = RewriteImagePaths(item.Content, item.FilePath, epub, imagesDirectory);
        html = InlineCssLinks(html, item.FilePath, epub);
        return html;
    }

    // Streams one image at a time over the lazy EpubBookRef: the eager ReadEpubSafeAsync would
    // already hold every image in memory before the first one is yielded (D-...-3).
    public async IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath)
    {
        using var bookRef = await OpenEpubSafeAsync(filePath);
        foreach (var image in bookRef.Content.Images.Local)
            yield return new ExtractedImage(image.FilePath, await image.ReadContentAsBytesAsync());
    }

    public async Task<byte[]?> ExtractCoverImageAsync(string filePath)
    {
        var epub = await ReadEpubSafeAsync(filePath);

        if (epub.CoverImage is { Length: > 0 })
            return epub.CoverImage;

        if (epub.Content.Cover?.Content is { Length: > 0 } coverContent)
            return coverContent;

        return FindCoverInManifest(epub);
    }

    public async Task<string> CreateTranslatedEpubAsync(
        string originalFilePath,
        string translatedTitle,
        IReadOnlyDictionary<string, string> translatedChapterHtml,
        string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        var shortGuid = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{Path.GetFileNameWithoutExtension(originalFilePath)}_translated_{shortGuid}.epub";
        var destPath = Path.Combine(destinationDirectory, fileName);
        File.Copy(originalFilePath, destPath, overwrite: true);

        await using (var archive = await ZipFile.OpenAsync(destPath, ZipArchiveMode.Update))
        {
            foreach (var (href, html) in translatedChapterHtml)
            {
                var normalizedHref = href.Replace('\\', '/');
                var entry = archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.FullName.Replace('\\', '/'), normalizedHref, StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.Replace('\\', '/').EndsWith("/" + normalizedHref, StringComparison.OrdinalIgnoreCase));
                if (entry is null) continue;

                await using var stream = await entry.OpenAsync();
                stream.SetLength(0);
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(html);
            }

            await UpdateOpfTitleAsync(archive, translatedTitle);
        }

        return destPath;
    }

    private static async Task UpdateOpfTitleAsync(ZipArchive archive, string newTitle)
    {
        var opfEntry = archive.Entries.FirstOrDefault(e =>
            e.FullName.EndsWith(".opf", StringComparison.OrdinalIgnoreCase));
        if (opfEntry is null) return;

        string content;
        await using (var readStream = await opfEntry.OpenAsync())
        {
            using var reader = new StreamReader(readStream);
            content = await reader.ReadToEndAsync();
        }

        var escapedTitle = System.Net.WebUtility.HtmlEncode(newTitle);
        content = OpfTitleRegex().Replace(content, $"$1{escapedTitle}$3");

        await using var stream = await opfEntry.OpenAsync();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content);
        await writer.FlushAsync();
    }

    private static async Task<EpubBook> ReadEpubSafeAsync(string filePath)
    {
        try
        {
            return (await EpubReader.ReadBookAsync(filePath, BuildStrictOptions()))!;
        }
        catch (EpubPackageException)
        {
            return (await EpubReader.ReadBookAsync(filePath, BuildFallbackOptions()))!;
        }
    }

    // Lazy twin of ReadEpubSafeAsync, kept separate because C# forbids yield return inside a
    // try/catch: the shared option builders are what make "same tolerance" structural, not copied.
    private static async Task<EpubBookRef> OpenEpubSafeAsync(string filePath)
    {
        try
        {
            return (await EpubReader.OpenBookAsync(filePath, BuildStrictOptions()))!;
        }
        catch (EpubPackageException)
        {
            return (await EpubReader.OpenBookAsync(filePath, BuildFallbackOptions()))!;
        }
    }

    private static EpubReaderOptions BuildStrictOptions() => new()
    {
        PackageReaderOptions = new PackageReaderOptions
        {
            SkipInvalidManifestItems = true,
            SkipInvalidSpineItems = true,
            IgnoreMissingToc = true
        },
        BookCoverReaderOptions = new BookCoverReaderOptions
        {
            Epub2MetadataIgnoreMissingManifestItem = true,
            Epub2MetadataIgnoreMissingContentFile = true,
            Epub3IgnoreMissingContentFile = true
        },
        ContentReaderOptions = new ContentReaderOptions
        {
            IgnoreMissingFileError = true
        }
    };

    private static EpubReaderOptions BuildFallbackOptions() => new()
    {
        PackageReaderOptions = new PackageReaderOptions
        {
            SkipInvalidManifestItems = true,
            SkipInvalidSpineItems = true,
            IgnoreMissingToc = true,
            IgnoreMissingMetadataNode = true
        },
        BookCoverReaderOptions = new BookCoverReaderOptions
        {
            Epub2MetadataIgnoreMissingManifestItem = true,
            Epub2MetadataIgnoreMissingContentFile = true,
            Epub3IgnoreMissingContentFile = true,
            IgnoreRemoteContentFileError = true
        },
        ContentReaderOptions = new ContentReaderOptions
        {
            IgnoreMissingFileError = true,
            IgnoreFileIsTooLargeError = true
        }
    };

    private static string InlineCssLinks(string html, string chapterFilePath, EpubBook epub)
    {
        var chapterDir = GetDirectoryPath(chapterFilePath);

        return LinkTagRegex().Replace(html, match =>
        {
            var attrs = match.Groups[1].Value;
            if (!StylesheetRelRegex().IsMatch(attrs))
                return match.Value;

            var hrefMatch = StylesheetHrefRegex().Match(attrs);
            if (!hrefMatch.Success)
                return match.Value;

            var resolvedPath = ResolvePath(chapterDir, hrefMatch.Groups[1].Value);
            var cssFile = FindCss(epub, resolvedPath);
            if (cssFile is null)
                return match.Value;

            return $"<style type=\"text/css\">{cssFile.Content}</style>";
        });
    }

    private static EpubLocalTextContentFile? FindCss(EpubBook epub, string resolvedPath)
    {
        return epub.Content.Css.Local.FirstOrDefault(f =>
            string.Equals(f.FilePath, resolvedPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.Key, resolvedPath, StringComparison.OrdinalIgnoreCase)
            || f.FilePath.EndsWith("/" + resolvedPath, StringComparison.OrdinalIgnoreCase)
            || resolvedPath.EndsWith("/" + f.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    private static string RewriteImagePaths(string html, string chapterFilePath, EpubBook epub, string imagesDirectory)
    {
        var chapterDir = GetDirectoryPath(chapterFilePath);

        html = ImgSrcRegex().Replace(html, match =>
            ReplaceImageRef(match, 2, chapterDir, epub, imagesDirectory));

        html = SvgImageXlinkHrefRegex().Replace(html, match =>
            ReplaceImageRef(match, 2, chapterDir, epub, imagesDirectory));

        html = SvgImageHrefRegex().Replace(html, match =>
            ReplaceImageRef(match, 2, chapterDir, epub, imagesDirectory));

        return html;
    }

    private static string ReplaceImageRef(Match match, int srcGroup, string chapterDir, EpubBook epub, string imagesDirectory)
    {
        var src = match.Groups[srcGroup].Value;
        if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return match.Value;

        var resolvedPath = ResolvePath(chapterDir, src);
        var image = FindImage(epub, resolvedPath);
        if (image is null)
            return match.Value;

        var bookDir = Path.GetFileName(imagesDirectory);
        var imageUrl = $"https://epub-images/{bookDir}/{resolvedPath}";

        return $"{match.Groups[1].Value}{imageUrl}{match.Groups[3].Value}";
    }

    private static EpubLocalByteContentFile? FindImage(EpubBook epub, string resolvedPath)
    {
        return epub.Content.Images.Local.FirstOrDefault(img =>
            string.Equals(img.FilePath, resolvedPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(img.Key, resolvedPath, StringComparison.OrdinalIgnoreCase)
            || img.FilePath.EndsWith("/" + resolvedPath, StringComparison.OrdinalIgnoreCase)
            || resolvedPath.EndsWith("/" + img.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolvePath(string chapterDir, string src)
    {
        var combined = string.IsNullOrEmpty(chapterDir) ? src : chapterDir + "/" + src;
        return NormalizePath(combined);
    }

    private static string NormalizePath(string path)
    {
        var parts = new List<string>();
        foreach (var segment in path.Replace('\\', '/').Split('/'))
        {
            if (segment == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
            }
            else if (segment != "." && segment.Length > 0)
            {
                parts.Add(segment);
            }
        }
        return string.Join("/", parts);
    }

    private static string GetDirectoryPath(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized[..lastSlash] : string.Empty;
    }

    private static byte[]? FindCoverInManifest(EpubBook epub)
    {
        var manifestItems = epub.Schema.Package.Manifest.Items;

        var coverItem = manifestItems.FirstOrDefault(i =>
            i.Properties?.Contains(VersOne.Epub.Schema.EpubManifestProperty.COVER_IMAGE) == true)
            ?? manifestItems.FirstOrDefault(i =>
                string.Equals(i.Id, "cover", StringComparison.OrdinalIgnoreCase)
                && i.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            ?? manifestItems.FirstOrDefault(i =>
                i.Href.Contains("cover", StringComparison.OrdinalIgnoreCase)
                && i.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true);

        if (coverItem is null)
            return null;

        var imageFile = epub.Content.Images.Local
            .FirstOrDefault(img => img.Key == coverItem.Href || img.FilePath == coverItem.Href);

        return imageFile?.Content;
    }

    private static string ResolveChapterTitle(EpubBook epub, string filePath)
    {
        var navPoint = epub.Navigation?.FirstOrDefault(n => n.HtmlContentFile?.FilePath == filePath);
        return navPoint?.Title ?? Path.GetFileNameWithoutExtension(filePath);
    }

    [GeneratedRegex(@"(<dc:title[^>]*>)(.*?)(</dc:title>)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OpfTitleRegex();

    [GeneratedRegex(@"<link\b([^>]*?)/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTagRegex();

    [GeneratedRegex(@"\brel\s*=\s*""stylesheet""", RegexOptions.IgnoreCase)]
    private static partial Regex StylesheetRelRegex();

    [GeneratedRegex(@"\bhref\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex StylesheetHrefRegex();

    [GeneratedRegex(@"(<img\b[^>]*?\bsrc\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]
    private static partial Regex ImgSrcRegex();

    [GeneratedRegex(@"(<image\b[^>]*?\bxlink:href\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]
    private static partial Regex SvgImageXlinkHrefRegex();

    [GeneratedRegex(@"(<image\b[^>]*?\bhref\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]
    private static partial Regex SvgImageHrefRegex();
}
