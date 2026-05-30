namespace Specforge.Core.Ledger;

/// <summary>One data row of a ledger markdown table: the trimmed cell values, left to right.</summary>
/// <param name="Cells">Cell values (surrounding pipes/spaces already stripped).</param>
public sealed record LedgerRow(IReadOnlyList<string> Cells)
{
    /// <summary>The normalized first-column value (the LedgerId), backticks and spaces stripped.</summary>
    public string LedgerId => Cells.Count > 0 ? Normalize(Cells[0]) : string.Empty;

    /// <summary>Strips surrounding whitespace and Markdown code-span backticks from a cell value.</summary>
    public static string Normalize(string cell) => cell.Trim().Trim('`').Trim();
}
