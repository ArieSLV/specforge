namespace Specforge.Core.Identifiers;

/// <summary>
/// Computes the next free number for a kind within a package (DEC-004 counter advancement).
/// Interface-first so ITEM-010 can wrap it with a tombstone-aware decorator without changing callers.
/// </summary>
public interface IIdAllocator
{
    /// <summary>
    /// Returns <c>highest-in-use + 1</c> for <paramref name="kind"/> in <paramref name="packageName"/>,
    /// scanning the relevant ledger file. Throws <see cref="Exceptions.SpecforgeKindExhaustedException"/>
    /// when the result would exceed 999.
    /// </summary>
    Task<int> NextNumberAsync(string kind, string packageName, CancellationToken ct);
}
