namespace Specforge.Core.Ledger;

/// <summary>The outcome of an <see cref="ICommitLedgerService.AppendAsync"/> call.</summary>
/// <param name="Id">The allocated (or, for a dry run, the would-be) <c>CMT-NNN</c> id.</param>
/// <param name="PromotedCommitsLedger">
/// True when this append promoted <c>ART-LEDGER-COMMITS</c> from <c>Placeholder</c> to <c>Draft</c>
/// (or, for a dry run, when it would). One-time per package; false on every subsequent call.
/// </param>
public sealed record CommitAppendResult(string Id, bool PromotedCommitsLedger);

/// <summary>
/// Sequential commit-provenance operations over the active package's <c>ledger/commits.md</c>
/// (DEC-004 sequential <c>CMT-NNN</c>; ITEM-009 first writer). Allocation is tombstone-tolerant:
/// the next number is <c>max(existing) + 1</c>, so deleted rows leave a gap that is never reused.
/// </summary>
public interface ICommitLedgerService
{
    /// <summary>
    /// Appends a commit row linking <paramref name="targetId"/> (an <c>ART-*</c> id) to
    /// <paramref name="gitRef"/>; allocates the next global <c>CMT-NNN</c>. On the first ever append in
    /// a package, promotes the <c>ART-LEDGER-COMMITS</c> artifact row from <c>Placeholder</c> to
    /// <c>Draft</c>. When <paramref name="dryRun"/> is true, computes the result without writing.
    /// </summary>
    Task<CommitAppendResult> AppendAsync(string targetId, string gitRef, string detail, string? date, bool dryRun, CancellationToken ct);

    /// <summary>Removes the commit row with the given <c>CMT-NNN</c> id; returns whether a row was removed.</summary>
    Task<bool> RemoveAsync(string id, CancellationToken ct);

    /// <summary>The commit row with the given <c>CMT-NNN</c> id, or <see langword="null"/> if absent.</summary>
    Task<CommitRow?> FindByLedgerIdAsync(string id, CancellationToken ct);
}
