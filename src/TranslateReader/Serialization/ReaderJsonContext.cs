using System.Text.Json.Serialization;
using TranslateReader.Models;

namespace TranslateReader.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PageInfo))]
[JsonSerializable(typeof(ScrollInfo))]
[JsonSerializable(typeof(List<VisibleParagraph>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
internal partial class ReaderJsonContext : JsonSerializerContext;
