using Specforge.Core.Ledger;

using Xunit;

namespace Specforge.Tests.Ledger;

public class LedgerTableTests
{
    private const string Sample =
        "| LedgerId | Status |\n|---|---|\n| `DEC-001` | Approved |\n| `DEC-002` | Draft |\n";

    [Fact]
    public void Parse_ExtractsHeadersAndRows()
    {
        LedgerTable table = LedgerTableParser.Parse(Sample);

        Assert.Equal(["LedgerId", "Status"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("DEC-001", table.Rows[0].LedgerId);
        Assert.Equal("Approved", table.Rows[0].Cells[1]);
    }

    [Fact]
    public void FindByLedgerId_StripsBackticks()
    {
        LedgerTable table = LedgerTableParser.Parse(Sample);
        Assert.NotNull(table.FindByLedgerId("DEC-002"));
        Assert.Null(table.FindByLedgerId("DEC-999"));
    }

    [Fact]
    public void Write_RoundTripsStably()
    {
        LedgerTable parsed = LedgerTableParser.Parse(Sample);
        string written = LedgerTableWriter.Write(parsed);
        string rewritten = LedgerTableWriter.Write(LedgerTableParser.Parse(written));
        Assert.Equal(written, rewritten);
    }

    [Fact]
    public void Parse_NoTable_ReturnsEmpty() =>
        Assert.Empty(LedgerTableParser.Parse("# Heading\n\njust prose, no table\n").Rows);
}
