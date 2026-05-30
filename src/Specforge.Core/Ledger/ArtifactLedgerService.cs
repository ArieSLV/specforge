using Specforge.Core.Configuration;

namespace Specforge.Core.Ledger;

/// <summary>Default <see cref="IArtifactLedgerService"/> over the active package's <c>artifacts.md</c>.</summary>
public sealed class ArtifactLedgerService(SessionState session) : IArtifactLedgerService
{
    private static readonly string[] DefaultHeaders =
        ["LedgerId", "Artifact", "Status", "Depends on", "Owner / reviewer", "Last update", "Next action", "Blocking question"];

    public async Task<ArtifactRow?> FindByLedgerIdAsync(string id, CancellationToken ct)
    {
        LedgerDocument document = await LoadAsync(ct).ConfigureAwait(false);
        LedgerRow? row = document.Rows().FirstOrDefault(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        return row is null ? null : ArtifactRow.FromCells(row.Cells);
    }

    public async Task AppendAsync(ArtifactRow row, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(row);
        string path = Path(session);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        document.Append(row.ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(string id, Action<ArtifactRow> mutate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        string path = Path(session);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        LedgerRow? existing = document.Rows().FirstOrDefault(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        if (existing is null)
        {
            return false;
        }

        ArtifactRow row = ArtifactRow.FromCells(existing.Cells);
        mutate(row);
        document.Update(id, row.ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct)
    {
        string path = Path(session);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        int removed = document.Remove(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        if (removed > 0)
        {
            await document.SaveAsync(path, ct).ConfigureAwait(false);
        }

        return removed > 0;
    }

    private static string Path(SessionState session) => LedgerPaths.LedgerFile(session, "artifacts.md");

    private Task<LedgerDocument> LoadAsync(CancellationToken ct) => LedgerDocument.LoadAsync(Path(session), DefaultHeaders, ct);
}
