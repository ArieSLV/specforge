using Specforge.Core.Configuration;

namespace Specforge.Core.Ledger;

/// <summary>
/// Default <see cref="IArtifactLedgerService"/>. Target-table-aware (ITEM-008): rows whose LedgerId
/// starts with <c>ART-ITEM-</c> route to the active package's <c>ledger/items.md</c>; all other
/// <c>ART-*</c> rows route to <c>ledger/artifacts.md</c>.
/// </summary>
public sealed class ArtifactLedgerService(SessionState session) : IArtifactLedgerService
{
    private static readonly string[] ArtifactHeaders =
        ["LedgerId", "Artifact", "Status", "Depends on", "Owner / reviewer", "Last update", "Next action", "Blocking question"];

    private static readonly string[] ItemHeaders =
        ["LedgerId", "Item Spec", "Status", "Depends on", "Owner / reviewer", "Last update", "Next action", "Blocking question"];

    public async Task<ArtifactRow?> FindByLedgerIdAsync(string id, CancellationToken ct)
    {
        LedgerDocument document = await LoadAsync(id, ct).ConfigureAwait(false);
        LedgerRow? row = document.Rows().FirstOrDefault(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        return row is null ? null : ArtifactRow.FromCells(row.Cells);
    }

    public async Task AppendAsync(ArtifactRow row, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(row);
        (string path, string[] headers) = Routing(row.LedgerId);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, headers, ct).ConfigureAwait(false);
        document.Append(row.ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(string id, Action<ArtifactRow> mutate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        (string path, string[] headers) = Routing(id);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, headers, ct).ConfigureAwait(false);
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
        (string path, string[] headers) = Routing(id);
        LedgerDocument document = await LedgerDocument.LoadAsync(path, headers, ct).ConfigureAwait(false);
        int removed = document.Remove(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        if (removed > 0)
        {
            await document.SaveAsync(path, ct).ConfigureAwait(false);
        }

        return removed > 0;
    }

    private (string Path, string[] Headers) Routing(string ledgerId) =>
        ledgerId.StartsWith("ART-ITEM-", StringComparison.Ordinal)
            ? (LedgerPaths.LedgerFile(session, "items.md"), ItemHeaders)
            : (LedgerPaths.LedgerFile(session, "artifacts.md"), ArtifactHeaders);

    private Task<LedgerDocument> LoadAsync(string id, CancellationToken ct)
    {
        (string path, string[] headers) = Routing(id);
        return LedgerDocument.LoadAsync(path, headers, ct);
    }
}
