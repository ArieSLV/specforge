namespace Specforge.Core.Identifiers;

/// <summary>
/// Reads the first (LedgerId) column out of a ledger markdown table. The only ID-related component
/// that touches the filesystem, and only for reads. A missing file yields an empty list.
/// </summary>
public interface ILedgerReader
{
    /// <summary>Returns the LedgerId of each data row in the ledger file at <paramref name="ledgerFilePath"/>.</summary>
    Task<IReadOnlyList<string>> ReadLedgerIdsAsync(string ledgerFilePath, CancellationToken ct);
}

/// <summary>Concrete <see cref="ILedgerReader"/> parsing GitHub-flavored markdown tables.</summary>
public sealed class LedgerReader : ILedgerReader
{
    public async Task<IReadOnlyList<string>> ReadLedgerIdsAsync(string ledgerFilePath, CancellationToken ct)
    {
        if (!File.Exists(ledgerFilePath))
        {
            return [];
        }

        string[] lines = await File.ReadAllLinesAsync(ledgerFilePath, ct).ConfigureAwait(false);
        List<string> ids = [];
        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith('|'))
            {
                continue;
            }

            string[] cells = trimmed.Split('|');
            if (cells.Length < 2)
            {
                continue;
            }

            string first = cells[1].Trim().Trim('`').Trim();
            if (first.Length == 0
                || first.StartsWith("---", StringComparison.Ordinal)
                || first.Equals("LedgerId", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ids.Add(first);
        }

        return ids;
    }
}
