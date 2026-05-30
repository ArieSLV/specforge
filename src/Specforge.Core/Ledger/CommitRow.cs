namespace Specforge.Core.Ledger;

/// <summary>Typed view of a <c>commits.md</c> row (consumed by ITEM-009's append_commit / delete_commit).</summary>
public sealed record CommitRow(string LedgerId, string CommitSha, string Linked, string Branch, string Date, string Note)
{
    public IReadOnlyList<string> ToCells() => [$"`{LedgerId}`", $"`{CommitSha}`", Linked, Branch, Date, Note];

    public static CommitRow FromCells(IReadOnlyList<string> cells) => new(
        LedgerRow.Normalize(At(cells, 0)),
        LedgerRow.Normalize(At(cells, 1)),
        At(cells, 2),
        At(cells, 3),
        At(cells, 4),
        At(cells, 5));

    private static string At(IReadOnlyList<string> cells, int index) => index < cells.Count ? cells[index] : string.Empty;
}
