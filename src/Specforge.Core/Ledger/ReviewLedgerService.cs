using System.Globalization;

using Specforge.Core.Configuration;

namespace Specforge.Core.Ledger;

/// <summary>Default <see cref="IReviewLedgerService"/> over the active package's <c>reviews.md</c>.</summary>
public sealed class ReviewLedgerService(SessionState session) : IReviewLedgerService
{
    private static readonly string[] DefaultHeaders = ["LedgerId", "Target", "Reviewer", "Outcome", "Date", "Notes"];

    public async Task<string> AppendAsync(string targetId, string reviewer, string outcome, string? notes, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "reviews.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        int seq = NextSequence(document, targetId);
        string id = $"REV-{targetId}-{seq:D3}";
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        document.Append(new ReviewRow(id, targetId, reviewer, outcome, date, notes ?? string.Empty).ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);
        return id;
    }

    public async Task<IReadOnlyList<ReviewRow>> FindByTargetAsync(string targetId, CancellationToken ct)
    {
        LedgerDocument document = await LedgerDocument.LoadAsync(LedgerPaths.LedgerFile(session, "reviews.md"), DefaultHeaders, ct).ConfigureAwait(false);
        return [.. document.Rows().Select(r => ReviewRow.FromCells(r.Cells)).Where(r => string.Equals(r.Target, targetId, StringComparison.Ordinal))];
    }

    public async Task<int> RemoveByTargetAsync(string targetId, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "reviews.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        int removed = document.Remove(r => string.Equals(ReviewRow.FromCells(r.Cells).Target, targetId, StringComparison.Ordinal));
        if (removed > 0)
        {
            await document.SaveAsync(path, ct).ConfigureAwait(false);
        }

        return removed;
    }

    public async Task<bool> RemoveAsync(string compositeId, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "reviews.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        int removed = document.Remove(r => string.Equals(r.LedgerId, compositeId, StringComparison.Ordinal));
        if (removed > 0)
        {
            await document.SaveAsync(path, ct).ConfigureAwait(false);
        }

        return removed > 0;
    }

    private static int NextSequence(LedgerDocument document, string targetId)
    {
        int max = 0;
        foreach (LedgerRow row in document.Rows())
        {
            ReviewRow review = ReviewRow.FromCells(row.Cells);
            if (!string.Equals(review.Target, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts = review.LedgerId.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seq) && seq > max)
            {
                max = seq;
            }
        }

        return max + 1;
    }
}
