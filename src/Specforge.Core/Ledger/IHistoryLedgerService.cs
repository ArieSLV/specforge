namespace Specforge.Core.Ledger;

/// <summary>Append-only history events over the active package's <c>ledger/history.md</c>.</summary>
public interface IHistoryLedgerService
{
    /// <summary>Appends a <c>(date, targetId, event, detail)</c> row. Date is today's UTC date (ISO).</summary>
    Task AppendAsync(string targetId, string eventName, string detail, CancellationToken ct);

    /// <summary>
    /// Appends a history row whose target is one of the closed <see cref="HistoryLiteralTarget.AcceptedLiterals"/>
    /// (<c>(milestone)</c> / <c>(multiple)</c>). Throws <see cref="Exceptions.SpecforgeInvalidIdentifierException"/>
    /// for any other string — the literal set is closed (ITEM-009).
    /// </summary>
    Task AppendLiteralAsync(string literalTarget, string eventName, string detail, CancellationToken ct);
}
