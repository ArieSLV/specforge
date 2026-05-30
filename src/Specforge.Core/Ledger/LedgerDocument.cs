namespace Specforge.Core.Ledger;

/// <summary>
/// A loaded ledger file that supports byte-stable, minimal-diff row surgery: append/update/remove
/// individual rows without reformatting the rest of the file. Saved atomically (temp + rename).
/// </summary>
public sealed class LedgerDocument
{
    private readonly List<string> _lines;
    private readonly int _separatorIndex;

    private LedgerDocument(List<string> lines, int headerIndex, IReadOnlyList<string> headers)
    {
        _lines = lines;
        _separatorIndex = headerIndex + 1;
        Headers = headers;
    }

    /// <summary>Column headers of the table.</summary>
    public IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// Loads <paramref name="path"/>; if it has no table yet, appends a fresh header + separator built
    /// from <paramref name="defaultHeaders"/> (preserving any existing heading content above it).
    /// </summary>
    public static async Task<LedgerDocument> LoadAsync(string path, IReadOnlyList<string> defaultHeaders, CancellationToken ct)
    {
        List<string> lines = File.Exists(path)
            ? [.. (await File.ReadAllTextAsync(path, ct).ConfigureAwait(false)).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')]
            : [];

        int header = LocateHeader(lines);
        if (header < 0)
        {
            if (lines.Count > 0 && lines[^1].Length != 0)
            {
                lines.Add(string.Empty);
            }

            header = lines.Count;
            lines.Add(LedgerTableWriter.Row(defaultHeaders));
            lines.Add(LedgerTableWriter.Separator(defaultHeaders.Count));
        }

        return new LedgerDocument(lines, header, LedgerTableParser.SplitRow(lines[header]));
    }

    /// <summary>Current data rows.</summary>
    public IReadOnlyList<LedgerRow> Rows()
    {
        List<LedgerRow> rows = [];
        for (int i = _separatorIndex + 1; i < _lines.Count && LedgerTableParser.IsTableLine(_lines[i]); i++)
        {
            rows.Add(new LedgerRow(LedgerTableParser.SplitRow(_lines[i])));
        }

        return rows;
    }

    /// <summary>Appends a row after the last contiguous table row.</summary>
    public void Append(IReadOnlyList<string> cells) => _lines.Insert(LastRowLine() + 1, LedgerTableWriter.Row(cells));

    /// <summary>Replaces the row whose LedgerId equals <paramref name="id"/>; returns false if not found.</summary>
    public bool Update(string id, IReadOnlyList<string> cells)
    {
        int index = FindRowLine(id);
        if (index < 0)
        {
            return false;
        }

        _lines[index] = LedgerTableWriter.Row(cells);
        return true;
    }

    /// <summary>Removes every row matching <paramref name="predicate"/>; returns the count removed.</summary>
    public int Remove(Func<LedgerRow, bool> predicate)
    {
        int removed = 0;
        for (int i = _lines.Count - 1; i > _separatorIndex; i--)
        {
            if (LedgerTableParser.IsTableLine(_lines[i]) && predicate(new LedgerRow(LedgerTableParser.SplitRow(_lines[i]))))
            {
                _lines.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Atomically writes the file (temp + rename), creating parent directories as needed.</summary>
    public async Task SaveAsync(string path, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        string temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, string.Join('\n', _lines), ct).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
    }

    private int LastRowLine()
    {
        int last = _separatorIndex;
        for (int i = _separatorIndex + 1; i < _lines.Count && LedgerTableParser.IsTableLine(_lines[i]); i++)
        {
            last = i;
        }

        return last;
    }

    private int FindRowLine(string id)
    {
        for (int i = _separatorIndex + 1; i < _lines.Count && LedgerTableParser.IsTableLine(_lines[i]); i++)
        {
            if (string.Equals(new LedgerRow(LedgerTableParser.SplitRow(_lines[i])).LedgerId, id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static int LocateHeader(List<string> lines)
    {
        for (int i = 0; i + 1 < lines.Count; i++)
        {
            if (LedgerTableParser.IsTableLine(lines[i]) && LedgerTableParser.IsSeparator(lines[i + 1]))
            {
                return i;
            }
        }

        return -1;
    }
}
