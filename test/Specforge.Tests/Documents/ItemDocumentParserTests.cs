using Specforge.Core.Documents;

using Xunit;

namespace Specforge.Tests.Documents;

public class ItemDocumentParserTests
{
    private static string Complete()
    {
        System.Text.StringBuilder sb = new();
        sb.Append("# ITEM-001-SOLUTION-BOOTSTRAP - Solution Bootstrap\n\n");
        sb.Append("Status: Approved\nReview owner: User\nDepends on: `DEC-001`, `DEC-003`\nUpdates ledger rows: new ART-ITEM-001\n\n");
        foreach (string s in RequiredItemSections.FullTemplate)
        {
            sb.Append($"## {s}\n\nbody\n\n");
        }

        return sb.ToString();
    }

    [Fact]
    public void Parse_ExtractsHeaderAndLists()
    {
        ItemDocument doc = ItemDocumentParser.Parse(Complete(), "/x/ITEM-001-SOLUTION-BOOTSTRAP.md");

        Assert.Equal("ITEM-001", doc.Id);
        Assert.Equal("Solution Bootstrap", doc.Title);
        Assert.Equal("Approved", doc.Status);
        Assert.Equal("User", doc.ReviewOwner);
        Assert.Equal(["DEC-001", "DEC-003"], doc.DependsOn);
        Assert.Contains(doc.UpdatesLedgerRows, x => x.Contains("ART-ITEM-001", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_CompleteItem_HasNoMissingSections() =>
        Assert.Empty(ItemDocumentParser.Parse(Complete(), "/x/ITEM-001.md").MissingRequiredSections);

    [Fact]
    public void Parse_IncompleteItem_ReportsMissing()
    {
        const string md = "# ITEM-009-PARTIAL - Partial\n\nStatus: Draft\n\n## Handoff Summary\n\nx\n";
        ItemDocument doc = ItemDocumentParser.Parse(md, "/x/ITEM-009.md");
        Assert.Contains("Done Criteria", doc.MissingRequiredSections);
        Assert.DoesNotContain("Handoff Summary", doc.MissingRequiredSections);
    }

    [Fact]
    public void Parse_MissingHeading_Throws() =>
        Assert.Throws<FormatException>(() => ItemDocumentParser.Parse("no heading\n", "/x/bad.md"));
}
