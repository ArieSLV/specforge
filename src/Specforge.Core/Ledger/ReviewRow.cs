namespace Specforge.Core.Ledger;

/// <summary>Typed view of a <c>reviews.md</c> row (composite REV LedgerId + target + outcome).</summary>
public sealed record ReviewRow(string LedgerId, string Target, string Reviewer, string Outcome, string Date, string Notes)
{
    public IReadOnlyList<string> ToCells() => [$"`{LedgerId}`", $"`{Target}`", Reviewer, Outcome, Date, Notes];

    public static ReviewRow FromCells(IReadOnlyList<string> cells) => new(
        LedgerRow.Normalize(At(cells, 0)),
        LedgerRow.Normalize(At(cells, 1)),
        At(cells, 2),
        At(cells, 3),
        At(cells, 4),
        At(cells, 5));

    private static string At(IReadOnlyList<string> cells, int index) => index < cells.Count ? cells[index] : string.Empty;
}
