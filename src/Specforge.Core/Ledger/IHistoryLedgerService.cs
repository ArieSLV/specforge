namespace Specforge.Core.Ledger;

/// <summary>Append-only history events over the active package's <c>ledger/history.md</c>.</summary>
public interface IHistoryLedgerService
{
    /// <summary>Appends a <c>(date, targetId, event, detail)</c> row. Date is today's UTC date (ISO).</summary>
    Task AppendAsync(string targetId, string eventName, string detail, CancellationToken ct);
}
