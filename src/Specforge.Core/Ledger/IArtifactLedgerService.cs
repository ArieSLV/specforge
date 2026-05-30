namespace Specforge.Core.Ledger;

/// <summary>Row-by-LedgerId operations over the active package's <c>ledger/artifacts.md</c>.</summary>
public interface IArtifactLedgerService
{
    Task<ArtifactRow?> FindByLedgerIdAsync(string id, CancellationToken ct);

    Task AppendAsync(ArtifactRow row, CancellationToken ct);

    Task<bool> UpdateAsync(string id, Action<ArtifactRow> mutate, CancellationToken ct);

    Task<bool> RemoveAsync(string id, CancellationToken ct);
}
