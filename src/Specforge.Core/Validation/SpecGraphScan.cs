using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Ledger;

namespace Specforge.Core.Validation;

/// <summary>
/// Shared read helpers for the aspect validators (ITEM-010): loads ledger tables and parses the
/// decision/item files of the active package. Ill-formed files are skipped (the live spec graph parses
/// cleanly; markdown well-formedness lint is out of MVP scope per the item).
/// </summary>
internal static class SpecGraphScan
{
    public static async Task<LedgerTable> TableAsync(SessionState session, string fileName, CancellationToken ct)
    {
        string path = LedgerPaths.LedgerFile(session, fileName);
        return File.Exists(path)
            ? LedgerTableParser.Parse(await File.ReadAllTextAsync(path, ct).ConfigureAwait(false))
            : LedgerTable.Empty;
    }

    public static async Task<IReadOnlyList<DecisionDocument>> DecisionsAsync(SessionState session, CancellationToken ct)
    {
        List<DecisionDocument> docs = [];
        string directory = LedgerPaths.DecisionsDirectory(session);
        if (!Directory.Exists(directory))
        {
            return docs;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "DEC-*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                docs.Add(DecisionDocumentParser.Parse(await File.ReadAllTextAsync(file, ct).ConfigureAwait(false), file));
            }
            catch (FormatException)
            {
                // skip ill-formed file
            }
        }

        return docs;
    }

    public static async Task<IReadOnlyList<ItemDocument>> ItemsAsync(SessionState session, CancellationToken ct)
    {
        List<ItemDocument> docs = [];
        string directory = LedgerPaths.ItemsDirectory(session);
        if (!Directory.Exists(directory))
        {
            return docs;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "ITEM-*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            try
            {
                docs.Add(ItemDocumentParser.Parse(await File.ReadAllTextAsync(file, ct).ConfigureAwait(false), file));
            }
            catch (FormatException)
            {
                // skip ill-formed file
            }
        }

        return docs;
    }

    /// <summary>The normalized first-column LedgerIds of a table (non-empty).</summary>
    public static IReadOnlyList<string> RowIds(LedgerTable table) =>
        [.. table.Rows.Select(r => r.LedgerId).Where(id => id.Length > 0)];

    /// <summary>Parses the trailing 3-digit seq from ids of the form <c>&lt;prefix&gt;NNN</c> (e.g. ART-DEC-001, CMT-001).</summary>
    public static IReadOnlyList<int> SeqsWithPrefix(IEnumerable<string> ids, string prefix)
    {
        List<int> seqs = [];
        foreach (string id in ids)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal))
            {
                string rest = id[prefix.Length..];
                if (rest.Length == 3 && int.TryParse(rest, out int seq))
                {
                    seqs.Add(seq);
                }
            }
        }

        return seqs;
    }
}
