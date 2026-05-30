namespace Specforge.Core.Ledger;

/// <summary>
/// Parses a GitHub-flavored Markdown pipe table into a <see cref="LedgerTable"/>. Line-based (not
/// AST-based) so callers can perform byte-stable, minimal-diff row surgery on the original file.
/// </summary>
public static class LedgerTableParser
{
    /// <summary>Parses the first pipe table found in <paramref name="markdown"/>; returns <see cref="LedgerTable.Empty"/> if none.</summary>
    public static LedgerTable Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        string[] lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int header = LocateHeader(lines);
        if (header < 0)
        {
            return LedgerTable.Empty;
        }

        IReadOnlyList<string> headers = SplitRow(lines[header]);
        List<LedgerRow> rows = [];
        for (int i = header + 2; i < lines.Length && IsTableLine(lines[i]); i++)
        {
            rows.Add(new LedgerRow(SplitRow(lines[i])));
        }

        return new LedgerTable(headers, rows);
    }

    /// <summary>True if the line is part of a pipe table (its first non-space char is <c>|</c>).</summary>
    public static bool IsTableLine(string line) => line.TrimStart().StartsWith('|');

    /// <summary>True if the line is a table separator row (e.g. <c>|---|---|</c>).</summary>
    public static bool IsSeparator(string line)
    {
        string trimmed = line.Trim();
        if (!trimmed.StartsWith('|') || !trimmed.Contains('-', StringComparison.Ordinal))
        {
            return false;
        }

        return trimmed.All(c => c is '|' or '-' or ':' or ' ');
    }

    /// <summary>Splits a table line into trimmed cell values (drops the leading/trailing pipe).</summary>
    public static IReadOnlyList<string> SplitRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return [.. trimmed.Split('|').Select(c => c.Trim())];
    }

    private static int LocateHeader(string[] lines)
    {
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            if (IsTableLine(lines[i]) && IsSeparator(lines[i + 1]))
            {
                return i;
            }
        }

        return -1;
    }
}
