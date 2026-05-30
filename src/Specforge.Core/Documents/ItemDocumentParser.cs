using System.Text.RegularExpressions;

using Markdig;
using Markdig.Syntax;

namespace Specforge.Core.Documents;

/// <summary>
/// Parses an item spec into an <see cref="ItemDocument"/> (Markdig AST for headings, like
/// <see cref="DecisionDocumentParser"/>) and records which required sections are missing
/// (per <see cref="RequiredItemSections"/>). Does not reject incomplete files — ITEM-010 gates on completeness.
/// </summary>
public static partial class ItemDocumentParser
{
    [GeneratedRegex("^([A-Z]{2,6}-[0-9]{3})")]
    private static partial Regex IdentifierPrefix();

    public static ItemDocument Parse(string markdown, string absolutePath)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        MarkdownDocument document = Markdown.Parse(normalized);
        List<HeadingBlock> headings = [.. document.Descendants<HeadingBlock>().OrderBy(h => h.Span.Start)];

        HeadingBlock title = headings.FirstOrDefault(h => h.Level == 1)
            ?? throw new FormatException("item file has no top-level (#) heading");

        string headingText = HeadingText(normalized, title);
        Match idMatch = IdentifierPrefix().Match(headingText);
        if (!idMatch.Success)
        {
            throw new FormatException($"item heading '{headingText}' does not start with a <KIND>-<NNN> identifier");
        }

        string id = idMatch.Value;
        int dash = headingText.IndexOf(" - ", StringComparison.Ordinal);
        string plainTitle = dash >= 0 ? headingText[(dash + 3)..].Trim() : headingText.Trim();

        HeadingBlock? firstSection = headings.FirstOrDefault(h => h.Level == 2);
        int headerEnd = firstSection?.Span.Start ?? normalized.Length;
        Dictionary<string, string> header = ParseHeader(Slice(normalized, title.Span.End + 1, headerEnd));

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

        List<string> missing = [.. RequiredItemSections.Names.Where(s => !sections.ContainsKey(s))];

        return new ItemDocument(
            id,
            plainTitle,
            Get(header, "Status"),
            Get(header, "Review owner"),
            SplitList(GetOrEmpty(header, "Depends on")),
            SplitList(GetOrEmpty(header, "Updates ledger rows")),
            sections,
            missing,
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

    private static Dictionary<string, string> ParseHeader(string text)
    {
        Dictionary<string, string> header = new(StringComparer.Ordinal);
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
            if (key.Length > 0 && !header.ContainsKey(key))
            {
                header[key] = value;
            }
        }

        return header;
    }

    private static IReadOnlyList<string> SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return [.. value.Split(',').Select(p => p.Trim().Trim('`').Trim()).Where(p => p.Length > 0)];
    }

    private static string Get(Dictionary<string, string> header, string key) => header.TryGetValue(key, out string? v) ? v : string.Empty;

    private static string GetOrEmpty(Dictionary<string, string> header, string key) => header.TryGetValue(key, out string? v) ? v : string.Empty;
}
