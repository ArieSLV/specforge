namespace Specforge.Core.Ledger;

/// <summary>Composite-review operations over the active package's <c>ledger/reviews.md</c> (DEC-004 per-target seq).</summary>
public interface IReviewLedgerService
{
    /// <summary>Appends a review, allocating the next <c>REV-&lt;target&gt;-NNN</c> seq; returns the composite id.</summary>
    Task<string> AppendAsync(string targetId, string reviewer, string outcome, string? notes, CancellationToken ct);

    /// <summary>All review rows whose target equals <paramref name="targetId"/>.</summary>
    Task<IReadOnlyList<ReviewRow>> FindByTargetAsync(string targetId, CancellationToken ct);

    /// <summary>Removes every review row targeting <paramref name="targetId"/> (delete cascade); returns the count.</summary>
    Task<int> RemoveByTargetAsync(string targetId, CancellationToken ct);

    /// <summary>Removes a single review by its composite id; returns whether a row was removed.</summary>
    Task<bool> RemoveAsync(string compositeId, CancellationToken ct);
}
