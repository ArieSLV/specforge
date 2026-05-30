using System.Globalization;

using Specforge.Core.Configuration;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;

namespace Specforge.Core.Validation;

/// <summary>
/// Decorator over <see cref="IIdAllocator"/> (ITEM-010) that adds tombstone-gap awareness on top of the
/// base "max + 1" allocation. <see cref="NextNumberAsync"/> delegates unchanged; <see cref="VerifyGapsAsync"/>
/// returns the seqs that are missing AND have no explaining <c>Deleted</c> tombstone in <c>history.md</c>.
/// </summary>
public sealed class TombstoneAwareIdAllocator(IIdAllocator inner, SessionState session) : IIdAllocator
{
    public Task<int> NextNumberAsync(string kind, string packageName, CancellationToken ct) =>
        inner.NextNumberAsync(kind, packageName, ct);

    /// <summary>
    /// Given the seqs observed for <paramref name="kind"/>, returns the unexplained gaps: missing numbers
    /// in <c>1..max(observed)</c> for which no <c>Tombstone &lt;kind&gt;-&lt;NNN&gt;:</c> history event exists.
    /// </summary>
    public async Task<IReadOnlyList<int>> VerifyGapsAsync(string kind, IReadOnlyList<int> observedSeqs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(observedSeqs);
        if (observedSeqs.Count == 0)
        {
            return [];
        }

        HashSet<int> present = [.. observedSeqs];
        int max = observedSeqs.Max();
        List<int> gaps = [.. Enumerable.Range(1, max).Where(n => !present.Contains(n))];
        if (gaps.Count == 0)
        {
            return [];
        }

        IReadOnlyCollection<string> tombstoned = await ReadTombstonedIdsAsync(ct).ConfigureAwait(false);
        return [.. gaps.Where(g => !tombstoned.Contains($"{kind}-{g.ToString("D3", CultureInfo.InvariantCulture)}"))];
    }

    /// <summary>The set of ids recorded as deleted by a <c>Deleted</c> history event with a tombstone-format detail.</summary>
    public async Task<IReadOnlyCollection<string>> ReadTombstonedIdsAsync(CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "history.md");
        if (!File.Exists(path))
        {
            return [];
        }

        LedgerTable table = LedgerTableParser.Parse(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false));
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (LedgerRow row in table.Rows)
        {
            if (row.Cells.Count >= 4
                && string.Equals(LedgerRow.Normalize(row.Cells[2]), "Deleted", StringComparison.Ordinal)
                && TombstoneDetailFormat.TryExtractDeletedId(row.Cells[3], out string? deleted)
                && deleted is not null)
            {
                ids.Add(deleted);
            }
        }

        return ids;
    }
}
