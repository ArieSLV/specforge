using System.Text.RegularExpressions;

using Markdig;
using Markdig.Syntax;

namespace Specforge.Core.Documents;

/// <summary>
/// Parses a decision record into a <see cref="DecisionDocument"/> using the Markdig AST to locate the
/// top-level heading and section headings (robust against <c>##</c> sequences inside fenced code).
/// </summary>
public static partial class DecisionDocumentParser
{
    [GeneratedRegex("^([A-Z]{2,6}-[0-9]{3})")]
    private static partial Regex IdentifierPrefix();

    /// <summary>Parses <paramref name="markdown"/>; throws <see cref="FormatException"/> on a missing/invalid heading.</summary>
    public static DecisionDocument Parse(string markdown, string absolutePath)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        MarkdownDocument document = Markdown.Parse(normalized);
        List<HeadingBlock> headings = [.. document.Descendants<HeadingBlock>().OrderBy(h => h.Span.Start)];

        HeadingBlock title = headings.FirstOrDefault(h => h.Level == 1)
            ?? throw new FormatException("decision file has no top-level (#) heading");

        string headingText = HeadingText(normalized, title);
        Match idMatch = IdentifierPrefix().Match(headingText);
        if (!idMatch.Success)
        {
            throw new FormatException($"decision heading '{headingText}' does not start with a <KIND>-<NNN> identifier");
        }

        string id = idMatch.Value;
        int dash = headingText.IndexOf(" - ", StringComparison.Ordinal);
        string plainTitle = dash >= 0 ? headingText[(dash + 3)..].Trim() : headingText.Trim();

        HeadingBlock? firstSection = headings.FirstOrDefault(h => h.Level == 2);
        int metadataEnd = firstSection?.Span.Start ?? normalized.Length;
        Dictionary<string, string> metadata = ParseMetadata(Slice(normalized, title.Span.End + 1, metadataEnd));

        Dictionary<string, string> sections = new(StringComparer.Ordinal);
        foreach (HeadingBlock section in headings.Where(h => h.Level == 2))
        {
            string name = HeadingText(normalized, section);
            int bodyEnd = headings
                .Where(h => h.Span.Start > section.Span.Start && h.Level <= 2)
                .Select(h => (int?)h.Span.Start)
                .FirstOrDefault() ?? normalized.Length;
            sections[name] = Slice(normalized, section.Span.End + 1, bodyEnd).Trim('\n').Trim();
        }

        return new DecisionDocument(
            id,
            plainTitle,
            Get(metadata, "Status"),
            Get(metadata, "Date"),
            Get(metadata, "Owner"),
            Get(metadata, "Review owner"),
            GetOrNull(metadata, "Supersedes"),
            GetOrNull(metadata, "Covers"),
            GetOrNull(metadata, "Amends"),
            sections,
            markdown,
            absolutePath);
    }

    private static string HeadingText(string markdown, HeadingBlock heading) =>
        Slice(markdown, heading.Span.Start, heading.Span.End + 1).TrimStart('#').Trim();

    private static string Slice(string source, int start, int end)
    {
        start = Math.Clamp(start, 0, source.Length);
        end = Math.Clamp(end, start, source.Length);
        return source[start..end];
    }

    private static Dictionary<string, string> ParseMetadata(string text)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal);
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            int colon = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }

            string key = trimmed[..colon].Trim();
            string value = trimmed[(colon + 1)..].Trim();
            if (key.Length > 0 && !metadata.ContainsKey(key))
            {
                metadata[key] = value;
            }
        }

        return metadata;
    }

    private static string Get(Dictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out string? value) ? value : string.Empty;

    private static string? GetOrNull(Dictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out string? value) ? value : null;
}
