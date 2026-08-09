using System.Text.Json.Serialization;
using TranslateReader.Models;

namespace TranslateReader.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PageInfo))]
[JsonSerializable(typeof(ScrollInfo))]
[JsonSerializable(typeof(List<VisibleParagraph>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(SnippetRequest))]
[JsonSerializable(typeof(List<SnippetRequest>))]
[JsonSerializable(typeof(SnippetToggleRequest))]
[JsonSerializable(typeof(SnippetRemoveRequest))]
[JsonSerializable(typeof(List<SnippetTranslation>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(SnippetLabels))]
internal partial class ReaderJsonContext : JsonSerializerContext;
