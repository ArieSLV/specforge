using System.Globalization;

using Specforge.Core.Configuration;

namespace Specforge.Core.Ledger;

/// <summary>Default <see cref="IHistoryLedgerService"/> over the active package's <c>history.md</c>.</summary>
public sealed class HistoryLedgerService(SessionState session) : IHistoryLedgerService
{
    private static readonly string[] DefaultHeaders = ["Date", "LedgerId", "Event", "Detail"];

    public async Task AppendAsync(string targetId, string eventName, string detail, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, "history.md");
        LedgerDocument document = await LedgerDocument.LoadAsync(path, DefaultHeaders, ct).ConfigureAwait(false);
        string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        document.Append(new HistoryRow(date, targetId, eventName, detail).ToCells());
        await document.SaveAsync(path, ct).ConfigureAwait(false);
    }
}
