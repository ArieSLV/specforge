namespace Specforge.Core.Ledger;

/// <summary>Typed view of a <c>commits.md</c> row (written by ITEM-009's append_commit, read by delete_commit).</summary>
public sealed record CommitRow(string LedgerId, string CommitSha, string Linked, string Branch, string Date, string Note)
{
    // LedgerId, CommitSha, Linked and Branch are rendered as Markdown code-spans to match the
    // established ledger style (e.g. `CMT-009` | `986efba` | `ART-ITEM-009` | `master`).
    public IReadOnlyList<string> ToCells() => [$"`{LedgerId}`", $"`{CommitSha}`", $"`{Linked}`", $"`{Branch}`", Date, Note];

    public static CommitRow FromCells(IReadOnlyList<string> cells) => new(
        LedgerRow.Normalize(At(cells, 0)),
        LedgerRow.Normalize(At(cells, 1)),
        LedgerRow.Normalize(At(cells, 2)),
        LedgerRow.Normalize(At(cells, 3)),
        At(cells, 4),
        At(cells, 5));

    private static string At(IReadOnlyList<string> cells, int index) => index < cells.Count ? cells[index] : string.Empty;
}
