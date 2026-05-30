namespace Specforge.Core.Ledger;

/// <summary>Typed view of an <c>artifacts.md</c> row (8 columns). Mutable for in-place status updates.</summary>
public sealed class ArtifactRow
{
    public string LedgerId { get; set; } = string.Empty;

    public string Artifact { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string DependsOn { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string LastUpdate { get; set; } = string.Empty;

    public string NextAction { get; set; } = string.Empty;

    public string BlockingQuestion { get; set; } = string.Empty;

    /// <summary>Renders the row's cells (LedgerId wrapped in backticks to match the ledger style).</summary>
    public IReadOnlyList<string> ToCells() =>
        [$"`{LedgerId}`", Artifact, Status, DependsOn, Owner, LastUpdate, NextAction, BlockingQuestion];

    public static ArtifactRow FromCells(IReadOnlyList<string> cells) => new()
    {
        LedgerId = LedgerRow.Normalize(At(cells, 0)),
        Artifact = At(cells, 1),
        Status = At(cells, 2),
        DependsOn = At(cells, 3),
        Owner = At(cells, 4),
        LastUpdate = At(cells, 5),
        NextAction = At(cells, 6),
        BlockingQuestion = At(cells, 7),
    };

    private static string At(IReadOnlyList<string> cells, int index) => index < cells.Count ? cells[index] : string.Empty;
}
