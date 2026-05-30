using System.Globalization;

using Specforge.Core.Configuration;
using Specforge.Core.Lifecycle;

namespace Specforge.Core.Ledger;

/// <summary>
/// Default <see cref="ICommitLedgerService"/> over the active package's <c>commits.md</c>. Owns the
/// global <c>CMT-NNN</c> allocation and the one-time <c>ART-LEDGER-COMMITS</c> Placeholder → Draft
/// promotion; the history events ("Implemented", promotion) are emitted by the calling tool.
/// </summary>
public sealed class CommitLedgerService(SessionState session, IArtifactLedgerService artifacts) : ICommitLedgerService
{
    /// <summary>The artifact row whose status reflects whether the commits ledger is in use.</summary>
    public const string CommitsLedgerArtifactId = "ART-LEDGER-COMMITS";

    // No branch argument flows through append_commit in the MVP; every dogfood row is on `master`.
    private const string DefaultBranch = "master";

    private static readonly string[] DefaultHeaders =
        ["LedgerId", "Commit SHA", "Linked artifacts/items", "Branch", "Date", "Note"];

    public async Task<CommitAppendResult> AppendAsync(string targetId, string gitRef, string detail, string? date, bool dryRun, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "commits.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        string id = $"CMT-{NextSequence(document):D3}";
        bool wouldPromote = await IsCommitsLedgerPlaceholderAsync(ct).ConfigureAwait(false);

        if (dryRun)
        {
            return new CommitAppendResult(id, wouldPromote);
        }

        string effectiveDate = date ?? Today();
        document.Append(new CommitRow(id, gitRef, targetId, DefaultBranch, effectiveDate, detail).ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);

        if (wouldPromote)
        {
            await artifacts.UpdateAsync(CommitsLedgerArtifactId, row =>
            {
                row.Status = LifecycleState.Draft;
                row.LastUpdate = $"{Today()}: promoted Placeholder → Draft on first append_commit";
            }, ct).ConfigureAwait(false);
        }

        return new CommitAppendResult(id, wouldPromote);
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "commits.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        int removed = document.Remove(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        if (removed > 0)
        {
            await document.SaveAsync(path, ct).ConfigureAwait(false);
        }

        return removed > 0;
    }

    public async Task<CommitRow?> FindByLedgerIdAsync(string id, CancellationToken ct)
    {
        LedgerDocument document = await LedgerDocument.LoadAsync(LedgerPaths.LedgerFile(session, "commits.md"), DefaultHeaders, ct).ConfigureAwait(false);
        LedgerRow? row = document.Rows().FirstOrDefault(r => string.Equals(r.LedgerId, id, StringComparison.Ordinal));
        return row is null ? null : CommitRow.FromCells(row.Cells);
    }

    private async Task<bool> IsCommitsLedgerPlaceholderAsync(CancellationToken ct)
    {
        ArtifactRow? row = await artifacts.FindByLedgerIdAsync(CommitsLedgerArtifactId, ct).ConfigureAwait(false);
        return row is not null && string.Equals(row.Status, LifecycleState.Placeholder, StringComparison.Ordinal);
    }

    private static int NextSequence(LedgerDocument document)
    {
        int max = 0;
        foreach (LedgerRow row in document.Rows())
        {
            string id = row.LedgerId;
            int dash = id.LastIndexOf('-');
            if (dash >= 0
                && int.TryParse(id[(dash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seq)
                && seq > max)
            {
                max = seq;
            }
        }

        return max + 1;
    }

    private static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
