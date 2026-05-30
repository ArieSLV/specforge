namespace Specforge.Core.Ledger;

/// <summary>Typed view of a <c>history.md</c> row (Date, LedgerId, Event, Detail). Append-only (DEC-004).</summary>
public sealed record HistoryRow(string Date, string LedgerId, string Event, string Detail)
{
    public IReadOnlyList<string> ToCells() => [Date, $"`{LedgerId}`", Event, Detail];
}
