using System.Text;

namespace Specforge.Core.Ledger;

/// <summary>Renders a <see cref="LedgerTable"/> back to canonical pipe-table markdown.</summary>
public static class LedgerTableWriter
{
    /// <summary>Renders the full table (header + separator + rows), one line each, trailing newline included.</summary>
    public static string Write(LedgerTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        StringBuilder builder = new();
        builder.Append(Row(table.Headers)).Append('\n');
        builder.Append(Separator(table.Headers.Count)).Append('\n');
        foreach (LedgerRow row in table.Rows)
        {
            builder.Append(Row(row.Cells)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Formats one row line: <c>| a | b | c |</c>.</summary>
    public static string Row(IReadOnlyList<string> cells) => "| " + string.Join(" | ", cells) + " |";

    /// <summary>Formats the separator line for <paramref name="columnCount"/> columns: <c>|---|---|</c>.</summary>
    public static string Separator(int columnCount) => "|" + string.Concat(Enumerable.Repeat("---|", columnCount));
}
