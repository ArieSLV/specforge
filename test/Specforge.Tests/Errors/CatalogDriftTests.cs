using Specforge.Core.Diagnostics;
using Specforge.Core.Ledger;

using Xunit;

namespace Specforge.Tests.Errors;

/// <summary>
/// The spec graph validating itself (ITEM-011): the DEC-007 Error-Code Catalog table (parsed with
/// ITEM-007's <see cref="LedgerTableParser"/>) must list exactly the codes in <see cref="SpecforgeErrorCode.AllCodes"/>.
/// </summary>
public class CatalogDriftTests
{
    private static string? FindDec007()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "spec", "packages", "specforge-mvp", "decisions", "DEC-007-MVP-TOOL-SET.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    [Fact]
    public void Dec007Catalog_MatchesErrorCodeStore_Bidirectionally()
    {
        string? path = FindDec007();
        Assert.True(path is not null, "DEC-007-MVP-TOOL-SET.md not found by walk-up from the test base directory");

        string text = File.ReadAllText(path!);
        int start = text.IndexOf("### Error-Code Catalog", StringComparison.Ordinal);
        Assert.True(start >= 0, "DEC-007 has no '### Error-Code Catalog' section");

        // The Error-Code Catalog table is the first pipe table in this section; the parser stops at the
        // first non-table line (the prose paragraph after the table).
        LedgerTable table = LedgerTableParser.Parse(text[start..]);
        HashSet<string> catalogCodes = [.. table.Rows.Select(r => r.LedgerId).Where(c => c.StartsWith("specforge.", StringComparison.Ordinal))];

        Assert.True(
            catalogCodes.SetEquals(SpecforgeErrorCode.AllCodes),
            $"catalog/store drift — in catalog only: [{string.Join(", ", catalogCodes.Except(SpecforgeErrorCode.AllCodes))}]; " +
            $"in store only: [{string.Join(", ", SpecforgeErrorCode.AllCodes.Except(catalogCodes))}]");
    }
}
