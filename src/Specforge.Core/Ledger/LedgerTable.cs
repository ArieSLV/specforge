namespace Specforge.Core.Ledger;

/// <summary>A parsed ledger markdown table: column headers plus the data rows.</summary>
/// <param name="Headers">Column header cell values.</param>
/// <param name="Rows">Data rows (the header + separator lines are excluded).</param>
public sealed record LedgerTable(IReadOnlyList<string> Headers, IReadOnlyList<LedgerRow> Rows)
{
    /// <summary>An empty table (no header found).</summary>
    public static LedgerTable Empty { get; } = new([], []);

    /// <summary>Finds the first row whose normalized LedgerId equals <paramref name="id"/>.</summary>
    public LedgerRow? FindByLedgerId(string id) =>
        Rows.FirstOrDefault(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
}
